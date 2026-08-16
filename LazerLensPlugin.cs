using System;
using System.Reflection;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Online;
using osu.Game.Overlays.Toolbar;
using osu.Game.Scoring;
using osucc.Celebrations;
using osucc.Client;
using osucc.Plugin;
using LazerLens.Models;
using LazerLens.Services;
using LazerLens.UI;

namespace LazerLens
{
    public class LazerLensPlugin : OsuCcPluginBase, IOsuCcIconProvider
    {
        public static LazerLensPlugin? Instance { get; private set; }

        public void LogMessage(string msg) => Host.Log(msg);

        public IconUsage? Icon => FontAwesome.Solid.ChartBar;

        private readonly LazerLensService trackerService = new();
        private LazerLensOverlay? overlay;
        private IDisposable? overlayRegistration;

        private static UserStatisticsWatcher? cachedWatcher;
        private static LocalUserStatisticsProvider? cachedStatsProvider;
        private bool isWatcherHooked;
        private bool isProviderHooked;
        private Action<osu.Framework.Bindables.ValueChangedEvent<osu.Game.Online.ScoreBasedUserStatisticsUpdate?>>? watcherAction;
        private Action<UserStatisticsUpdate>? providerAction;

        protected override void OnLoad()
        {
            Instance = this;
            Host.Log("LazerLens OnLoad() called. Initializing settings and patches...");

            var settings = Host.GetSettings();

            var notifySetting = settings.Bind("notify_on_play", true);
            var celebrateSetting = settings.Bind("celebrate_best", true);
            var retriesSetting = settings.Bind("track_retries", true);
            var compactSetting = settings.Bind("compact_mode", false);
            var showUrSetting = settings.Bind("show_ur", true);

            trackerService.NotifyOnPlay.BindTo(notifySetting);
            trackerService.CelebrateBest.BindTo(celebrateSetting);
            trackerService.TrackRetries.BindTo(retriesSetting);
            trackerService.CompactMode.BindTo(compactSetting);
            trackerService.ShowUR.BindTo(showUrSetting);

            trackerService.OnNewPlayRecorded += onNewPlayRecorded;

            LazerLensPatch.Install(Host);
            LazerLensPatch.OnScoreImported += onScoreImported;
            LazerLensPatch.OnScoreUpdated += onScoreUpdated;
            Host.Log("LazerLens OnLoad() complete.");
        }

        public override void AttachToGame()
        {
            Host.Log("LazerLens AttachToGame() called. Hooking into OsuGame and registering UI overlays...");
            EnsureHooked();

            // Attach VFS storage for session persistence
            trackerService.AttachStorage(Host.GetStorage());

            // 1. Instantiate the session overlay and register it with the game's overlay manager
            overlay = new LazerLensOverlay(trackerService);
            overlayRegistration = Host.RegisterBlockingOverlay(overlay);

            // 2. Add toolbar button
            Host.AddToolbarButton(
                () => new LazerLensToolbarButton(() => overlay.ToggleVisibility()),
                ToolbarButtonPlacement.Right,
                -2f
            );

            // 3. Register settings subsection
            Host.AddSettingsSubsection(() => new LazerLensSettingsSubsection(Host.GetSettings(), exportSessionsToCsv, trackerService.OpenSessionsDirectory));
            Host.Log("LazerLens AttachToGame() complete.");
        }

        private void exportSessionsToCsv()
        {
            var exportPath = trackerService.ExportSessionsToCsv();
            if (exportPath != null)
            {
                Host.Notify(
                    new osu.Framework.Localisation.LocalisableString($"Successfully exported sessions to:\n{exportPath}"),
                    osucc.Client.NotificationKind.Success
                );
            }
            else
            {
                Host.Notify(
                    new osu.Framework.Localisation.LocalisableString("Could not export sessions to CSV."),
                    osucc.Client.NotificationKind.Error
                );
            }
        }

        public void EnsureHooked()
        {
            if (!isWatcherHooked)
            {
                var watcher = GetWatcher();
                if (watcher != null)
                {
                    watcherAction = e =>
                    {
                        if (e.NewValue != null)
                        {
                            Host.Scheduler?.Add(() =>
                            {
                                trackerService.OnUserStatisticsUpdated(e.NewValue);
                            });
                        }
                    };
                    watcher.LatestUpdate.ValueChanged += watcherAction;

                    isWatcherHooked = true;
                }
            }

            if (!isProviderHooked)
            {
                var statsProvider = GetStatsProvider();
                if (statsProvider != null)
                {
                    providerAction = update =>
                    {
                        if (update?.OldStatistics != null && update.NewStatistics != null)
                        {
                            Host.Scheduler?.Add(() =>
                            {
                                trackerService.OnDirectStatisticsUpdated(update.OldStatistics, update.NewStatistics);
                            });
                        }
                    };
                    statsProvider.StatisticsUpdated += providerAction;

                    isProviderHooked = true;
                }
            }
        }

        public void CheckStatsOnResults()
        {
            EnsureHooked();

            var watcher = GetWatcher();
            if (watcher?.LatestUpdate?.Value != null)
            {
                trackerService.OnUserStatisticsUpdated(watcher.LatestUpdate.Value);
            }
        }

        public static UserStatisticsWatcher? GetWatcher()
        {
            if (cachedWatcher != null) return cachedWatcher;

            var game = ClientApi.Game;
            if (game == null) return null;

            var prop = game.GetType().GetProperty("UserStatisticsWatcher", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop?.GetValue(game) is UserStatisticsWatcher val1)
            {
                cachedWatcher = val1;
                return cachedWatcher;
            }

            Type? current = game.GetType();
            while (current != null)
            {
                foreach (var f in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (typeof(UserStatisticsWatcher).IsAssignableFrom(f.FieldType))
                    {
                        if (f.GetValue(game) is UserStatisticsWatcher val2)
                        {
                            cachedWatcher = val2;
                            return cachedWatcher;
                        }
                    }
                }
                current = current.BaseType;
            }

            cachedWatcher = game.Dependencies?.Get(typeof(UserStatisticsWatcher)) as UserStatisticsWatcher;
            return cachedWatcher;
        }

        public static LocalUserStatisticsProvider? GetStatsProvider()
        {
            if (cachedStatsProvider != null) return cachedStatsProvider;

            var game = ClientApi.Game;
            if (game == null) return null;

            Type? current = game.GetType();
            while (current != null)
            {
                foreach (var f in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (typeof(LocalUserStatisticsProvider).IsAssignableFrom(f.FieldType))
                    {
                        if (f.GetValue(game) is LocalUserStatisticsProvider val)
                        {
                            cachedStatsProvider = val;
                            return cachedStatsProvider;
                        }
                    }
                }
                current = current.BaseType;
            }

            cachedStatsProvider = game.Dependencies?.Get(typeof(LocalUserStatisticsProvider)) as LocalUserStatisticsProvider;
            return cachedStatsProvider;
        }

        private void onScoreImported(ScoreInfo score, bool passed, bool isRetry)
        {
            EnsureHooked();

            LazerLensPatch.DebugLog($"LazerLensPlugin: onScoreImported received score: Title={score.BeatmapInfo?.Metadata?.Title}, Passed={passed}, isRetry={isRetry}");

            try
            {
                trackerService.RecordScore(score, passed, isRetry);
                LazerLensPatch.DebugLog("LazerLensPlugin: RecordScore completed successfully.");
            }
            catch (Exception ex)
            {
                LazerLensPatch.DebugLog($"LazerLensPlugin: RecordScore threw exception: {ex}");
            }
        }

        private void onScoreUpdated(ScoreInfo score)
        {
            EnsureHooked();

            try
            {
                trackerService.UpdateScore(score);
            }
            catch (Exception ex)
            {
                LazerLensPatch.DebugLog($"LazerLensPlugin: UpdateScore threw exception: {ex}");
            }
        }

        private void onNewPlayRecorded(SessionPlayRecord play, bool isNewBest)
        {
            if (isNewBest && trackerService.CelebrateBest.Value)
            {
                ClientCelebrations.Show(new Celebration(new CelebrationOptions
                {
                    TitleText = "New Session Best!",
                    SubtitleText = $"{play.BeatmapArtist} - {play.BeatmapTitle}",
                    AccentColour = Color4Extensions.FromHex("00ffcc"),
                }));

                Host.Notify(
                    new LocalisableString($"New Session Best!\n{play.BeatmapArtist} - {play.BeatmapTitle} ({play.Accuracy:F2}%)"),
                    NotificationKind.Success
                );
            }
            else if (trackerService.NotifyOnPlay.Value && play.Passed)
            {
                string ppStr = play.PerformancePoints.HasValue ? $" \u2022 {play.PerformancePoints.Value:F0}pp" : "";

                Host.Notify(
                    new LocalisableString($"Added to tracker: {play.BeatmapTitle}\nAcc: {play.Accuracy:F2}% \u2022 Grade: {play.Grade}{ppStr}"),
                    NotificationKind.Info
                );
            }
        }

        public override void Dispose()
        {
            Host.Log("Disposing LazerLens plugin and unhooking events...");

            // Save current session before disposing
            trackerService.AutoSave();
            LazerLensPatch.OnScoreImported -= onScoreImported;
            LazerLensPatch.OnScoreUpdated -= onScoreUpdated;
            trackerService.OnNewPlayRecorded -= onNewPlayRecorded;

            trackerService.NotifyOnPlay.UnbindAll();
            trackerService.CelebrateBest.UnbindAll();
            trackerService.TrackRetries.UnbindAll();
            trackerService.CompactMode.UnbindAll();
            trackerService.ShowUR.UnbindAll();

            if (isWatcherHooked && watcherAction != null)
            {
                var watcher = GetWatcher();
                if (watcher != null)
                {
                    watcher.LatestUpdate.ValueChanged -= watcherAction;
                    Host.Log("Successfully unhooked from UserStatisticsWatcher.");
                }
                watcherAction = null;
                isWatcherHooked = false;
            }

            if (isProviderHooked && providerAction != null)
            {
                var provider = GetStatsProvider();
                if (provider != null)
                {
                    provider.StatisticsUpdated -= providerAction;
                    Host.Log("Successfully unhooked from LocalUserStatisticsProvider.");
                }
                providerAction = null;
                isProviderHooked = false;
            }

            overlayRegistration?.Dispose();
            overlayRegistration = null;
            
            overlay = null;
            Instance = null;
            
            Host.Log("Plugin disposal complete.");
            base.Dispose();
        }
    }
}

