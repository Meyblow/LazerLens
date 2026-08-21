using System;
using System.Globalization;
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
using LazerLens.Patches;
using LazerLens.Services;
using LazerLens.UI;
using osu.Game.Screens.Play;
using osucc.Core;

namespace LazerLens
{
    public class LazerLensPlugin : OsuCcPlugin
    {
        public static LazerLensPlugin? Instance { get; private set; }

        public void LogMessage(string msg) => Host.Log(msg);

        public override IconUsage? Icon => FontAwesome.Solid.ChartBar;

        private static readonly HashSet<int> recordedPlayerHashes = new();

        public override IReadOnlyList<OsuCcPatch> Patches => new OsuCcPatch[]
        {
            new PlayerImportScorePatch(this, Host),
            new PlayerConcludeFailedScorePatch(this, Host),
            new PlayerRestartPatch(this, Host),
            new PlayerPerformExitPatch(this, Host),
            new ResultsScreenLoadCompletePatch(this, Host),
        };

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

            int count = InstallPatches();
            Host.Log($"LazerLens: installed {count}/5 patches.");
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
            Host.AddSettingsSubsection(() => new LazerLensSettingsSubsection(Host.GetSettings(), ExportSessionsToCsv, trackerService.OpenSessionsDirectory));
            Host.Log("LazerLens AttachToGame() complete.");
        }

        public void ExportSessionsToCsv()
        {
            var exportPath = trackerService.ExportSessionsToCsv();
            if (exportPath != null)
            {
                Host.Notify(
                    LazerLensStrings.ExportSuccess(exportPath),
                    NotificationKind.Success
                );
            }
            else
            {
                Host.Notify(
                    LazerLensStrings.ExportFailed(""),
                    NotificationKind.Error
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

        public static bool TryMarkPlayerRecorded(Player player)
        {
            lock (recordedPlayerHashes)
            {
                int hash = player.GetHashCode();
                if (recordedPlayerHashes.Contains(hash))
                    return false;

                if (recordedPlayerHashes.Count > 300)
                    recordedPlayerHashes.Clear();

                recordedPlayerHashes.Add(hash);
                return true;
            }
        }

        public void RecordUnpassedPlayerScore(Player player)
        {
            try
            {
                if (player == null) return;

                string typeName = player.GetType().Name;
                if (typeName.Contains("Replay") || typeName.Contains("Spectator"))
                    return;

                if (!TryMarkPlayerRecorded(player))
                    return;

                if (player.GameplayState?.HasPassed == true)
                    return;

                var scoreInfo = player.Score?.ScoreInfo ?? player.GameplayState?.Score?.ScoreInfo ?? new ScoreInfo();

                if (scoreInfo.BeatmapInfo == null && player.GameplayState?.Beatmap != null)
                    scoreInfo.BeatmapInfo = player.GameplayState.Beatmap.BeatmapInfo;

                if (scoreInfo.BeatmapInfo == null && player.Beatmap?.Value != null)
                    scoreInfo.BeatmapInfo = player.Beatmap.Value.BeatmapInfo;

                if (scoreInfo.Ruleset == null && player.GameplayState?.Ruleset != null)
                    scoreInfo.Ruleset = player.GameplayState.Ruleset.RulesetInfo;

                if (scoreInfo.Ruleset == null && player.Ruleset?.Value != null)
                    scoreInfo.Ruleset = player.Ruleset.Value;

                if (scoreInfo.Mods == null || scoreInfo.Mods.Length == 0)
                {
                    if (player.GameplayState?.Mods != null)
                        scoreInfo.Mods = player.GameplayState.Mods.ToArray();
                    else if (player.Mods?.Value != null)
                        scoreInfo.Mods = player.Mods.Value.ToArray();
                    else
                        scoreInfo.Mods = Array.Empty<osu.Game.Rulesets.Mods.Mod>();
                }

                try
                {
                    var spProp = typeof(Player).GetProperty("ScoreProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var sp = spProp?.GetValue(player) as osu.Game.Rulesets.Scoring.ScoreProcessor;
                    sp?.PopulateScore(scoreInfo);
                }
                catch (Exception spEx)
                {
                    Host.Log(LogLevel.Error, $"LazerLens ScoreProcessor populate error: {spEx}");
                }

                scoreInfo.Date = DateTimeOffset.Now;
                scoreInfo.Rank = ScoreRank.F;

                OnScoreImported(scoreInfo, false);
            }
            catch (Exception ex)
            {
                Host.Log(LogLevel.Error, $"LazerLens RecordUnpassedPlayerScore error: {ex}");
            }
        }

        public void OnScoreImported(ScoreInfo score, bool passed)
        {
            EnsureHooked();

            try
            {
                trackerService.RecordScore(score, passed);
            }
            catch (Exception ex)
            {
                Host.Log(LogLevel.Error, $"LazerLens RecordScore exception: {ex}");
            }
        }

        public void OnScoreUpdated(ScoreInfo score)
        {
            EnsureHooked();

            try
            {
                trackerService.UpdateScore(score);
            }
            catch (Exception ex)
            {
                Host.Log(LogLevel.Error, $"LazerLens UpdateScore exception: {ex}");
            }
        }

        private void onNewPlayRecorded(SessionPlayRecord play, bool isNewBest)
        {
            if (isNewBest && trackerService.CelebrateBest.Value)
            {
                ClientCelebrations.Show(new Celebration(new CelebrationOptions
                {
                    TitleText = LazerLensStrings.NotificationNewBest.ToString(),
                    SubtitleText = $"{play.BeatmapArtist} - {play.BeatmapTitle}",
                    AccentColour = Color4Extensions.FromHex("00ffcc"),
                }));

                Host.Notify(
                    LazerLensStrings.NotificationNewBestDetails(play.BeatmapArtist, play.BeatmapTitle, play.Accuracy.ToString("F2", CultureInfo.InvariantCulture)),
                    NotificationKind.Success
                );
            }
            else if (trackerService.NotifyOnPlay.Value && play.Passed)
            {
                string ppStr = play.PerformancePoints.HasValue ? $" \u2022 {play.PerformancePoints.Value:F0}pp" : "";

                Host.Notify(
                    LazerLensStrings.AddedToTracker(play.BeatmapTitle, play.Accuracy.ToString("F2", CultureInfo.InvariantCulture), play.Grade, ppStr),
                    NotificationKind.Info
                );
            }
        }

        public override void Dispose()
        {
            Host.Log("Disposing LazerLens plugin and unhooking events...");

            // Save current session before disposing
            trackerService.AutoSave();
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
            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }
}
