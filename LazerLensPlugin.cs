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
using LazerLens.Utilities;
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
            var retriesSetting = settings.Bind("track_retries", false);
            var compactSetting = settings.Bind("compact_mode", true);
            var showUrSetting = settings.Bind("show_ur", true);

            trackerService.NotifyOnPlay.BindTo(notifySetting);
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

            // Attach VFS storage for session persistence (osu-cc/data/lazer-lens/sessions)
            trackerService.AttachStorage(Host.Data);

            // 1. Instantiate the session overlay and register it with the game's overlay manager
            overlay = new LazerLensOverlay(trackerService, ExportSessionsToCsv);
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

        public static UserStatisticsWatcher? GetWatcher() => ReflectionHelper.GetUserStatisticsWatcher();
        public static LocalUserStatisticsProvider? GetStatsProvider() => ReflectionHelper.GetLocalUserStatisticsProvider();

        public static bool TryMarkPlayerRecorded(Player player)
        {
            lock (recordedPlayerHashes)
            {
                int hash = player.GetHashCode();
                if (recordedPlayerHashes.Contains(hash))
                    return false;

                // Limit the hash cache size to 300 items to avoid unbounded memory growth during long gaming sessions
                if (recordedPlayerHashes.Count > 300)
                    recordedPlayerHashes.Clear();

                recordedPlayerHashes.Add(hash);
                return true;
            }
        }

        private static bool isPlayerFailed(Player player) => ReflectionHelper.IsPlayerFailed(player);

        public void RecordUnpassedPlayerScore(Player player, bool forceFailed = false)
        {
            try
            {
                if (player == null) return;

                string typeName = player.GetType().Name;
                if (typeName.Contains("Replay") || typeName.Contains("Spectator"))
                    return;

                bool isFailed = forceFailed || isPlayerFailed(player);
                if (!isFailed && !trackerService.TrackRetries.Value)
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

                ReflectionHelper.TryPopulateScoreProcessor(player, scoreInfo);

                if (scoreInfo.Date == default)
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

        private void onNewPlayRecorded(SessionPlayRecord play)
        {
            if (trackerService.NotifyOnPlay.Value && play.Passed)
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
