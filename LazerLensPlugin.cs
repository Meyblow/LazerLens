using System;
using System.Globalization;
using System.Reflection;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
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
            new PlayerPerformFailPatch(this, Host),
            new SubmittingPlayerConcludeFailedScorePatch(this, Host),
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

            // 1. Metrics & Display
            trackerService.DefaultSort.BindTo(settings.Bind("default_sort", DefaultSortMode.TimeDesc));
            trackerService.PpDisplay.BindTo(settings.Bind("pp_display", PpDisplayMode.Both));
            trackerService.AccuracyCalculation.BindTo(settings.Bind("accuracy_calc", AccuracyCalculationMode.ObjectWeighted));
            trackerService.HighlightUR.BindTo(settings.Bind("highlight_ur", true));
            trackerService.ShowModsInHistory.BindTo(settings.Bind("show_mods_history", true));
            trackerService.ShowDifficultyRating.BindTo(settings.Bind("show_difficulty_rating", true));
            trackerService.CompactMode.BindTo(settings.Bind("compact_mode", true));
            trackerService.ShowUR.BindTo(settings.Bind("show_ur", true));

            // 2. Session Management
            trackerService.SessionSplit.BindTo(settings.Bind("session_split", SessionSplitThreshold.Midnight));
            trackerService.AfkPause.BindTo(settings.Bind("afk_pause", AfkPauseTimeout.FiveMinutes));
            trackerService.EnableSessionPause.BindTo(settings.Bind("enable_session_pause", false));
            trackerService.AutoExportCsv.BindTo(settings.Bind("auto_export_csv", false));
            trackerService.ArchiveRetention.BindTo(settings.Bind("archive_retention", ArchiveRetentionLimit.Unlimited));

            // 3. Recording Filters
            trackerService.MinPlayDurationSeconds.BindTo(settings.Bind("min_play_duration", 5));
            trackerService.TrackStandard.BindTo(settings.Bind("track_standard", true));
            trackerService.TrackTaiko.BindTo(settings.Bind("track_taiko", true));
            trackerService.TrackCatch.BindTo(settings.Bind("track_catch", true));
            trackerService.TrackMania.BindTo(settings.Bind("track_mania", true));
            trackerService.TrackCustomRulesets.BindTo(settings.Bind("track_custom_rulesets", true));
            trackerService.TrackRetries.BindTo(settings.Bind("track_retries", false));
            trackerService.IgnoreNoFailPlays.BindTo(settings.Bind("ignore_nofail", false));
            trackerService.RankedLovedOnly.BindTo(settings.Bind("ranked_loved_only", false));

            // 4. Notifications & Milestones
            trackerService.PlayNotifFilter.BindTo(settings.Bind("notif_filter", PlayNotificationFilter.PassedOnly));
            trackerService.NotifySessionBest.BindTo(settings.Bind("notify_session_best", true));
            trackerService.Milestones.BindTo(settings.Bind("milestones", MilestoneNotificationMode.FiftyPlays));

            // 5. Overlay & Toolbar
            trackerService.AutoOpenOverlayOnPass.BindTo(settings.Bind("auto_open_overlay_on_pass", false));
            trackerService.ToolbarBadge.BindTo(settings.Bind("toolbar_badge", ToolbarBadgeMode.PlayCount));
            trackerService.ToolbarBadgeColor.BindTo(settings.Bind("toolbar_badge_color", "#00d2ff"));
            trackerService.SearchPosition.BindTo(settings.Bind("search_position", SearchBarPosition.Right));
            trackerService.OverlayWidth.BindTo(settings.Bind("overlay_width", 960));
            trackerService.OverlayBackdropOpacity.BindTo(settings.Bind("overlay_opacity", 0.9f));

            trackerService.OnNewPlayRecorded += onNewPlayRecorded;

            // Instantiate overlay early so the toolbar toggle button can track its visibility state
            overlay = new LazerLensOverlay(trackerService, ExportSessionsToCsv);

            // Register toolbar button and settings in OnLoad() before toolbar initialization
            Host.AddToolbarButton(
                () => new LazerLensToolbarButton(ToggleOverlay, trackerService, overlay),
                ToolbarButtonPlacement.Right,
                -2f
            );

            Host.AddSettingsSubsection(() => new LazerLensSettingsSubsection(Host.GetSettings(), trackerService, ExportSessionsToCsv, trackerService.OpenSessionsDirectory));

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

            if (overlay != null)
                overlayRegistration = Host.RegisterBlockingOverlay(overlay);

            Host.Log("LazerLens AttachToGame() complete.");
        }

        public void ToggleOverlay()
        {
            Host.Log($"LazerLens: ToggleOverlay called! overlay state = {(overlay == null ? "null" : overlay.State.Value.ToString())}");
            if (overlay == null)
                return;

            if (overlay.State.Value == osu.Framework.Graphics.Containers.Visibility.Hidden)
                overlay.Show();
            else
                overlay.Hide();
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
            if (trackerService.AutoOpenOverlayOnPass.Value && play.Passed)
            {
                Host.Scheduler?.Add(() =>
                {
                    if (overlay != null)
                    {
                        overlay.Show();
                        overlay.HighlightPlay(play.Id);
                    }
                });
            }

            if (play.Passed)
            {
                var best = trackerService.LiveState.BestScore;
                bool isNewBest = best != null && best.Id == play.Id && trackerService.LiveState.TotalPlays > 1;

                if (isNewBest && trackerService.NotifySessionBest.Value)
                {
                    string metric = play.PerformancePoints.HasValue && play.PerformancePoints.Value > 0
                        ? $"{play.PerformancePoints.Value:F0} PP \u2022 {play.Accuracy:F2}%"
                        : $"{play.TotalScore:N0} Score \u2022 {play.Accuracy:F2}%";

                    Host.Notify(
                        LazerLensStrings.ToastSessionBestBody(play.BeatmapTitle, metric),
                        NotificationKind.Success
                    );
                    return;
                }

                // Check milestones
                var milestone = trackerService.Milestones.Value;
                int totalPlays = trackerService.LiveState.TotalPlays;
                double ppGain = trackerService.LiveState.SessionPPGain;

                if (milestone == MilestoneNotificationMode.FiftyPlays && totalPlays > 0 && totalPlays % 50 == 0)
                {
                    Host.Notify(
                        LazerLensStrings.ToastMilestoneBody("50 beatmaps completed this session!"),
                        NotificationKind.Success
                    );
                }
                else if (milestone == MilestoneNotificationMode.HundredPlays && totalPlays > 0 && totalPlays % 100 == 0)
                {
                    Host.Notify(
                        LazerLensStrings.ToastMilestoneBody("100 beatmaps completed this session! Keep it up!"),
                        NotificationKind.Success
                    );
                }
                else if (milestone == MilestoneNotificationMode.FiftyPpGain && ppGain >= 50 && ppGain - (play.ProfilePerformancePoints ?? 0) < 50)
                {
                    Host.Notify(
                        LazerLensStrings.ToastMilestoneBody("+50 PP milestone reached today!"),
                        NotificationKind.Success
                    );
                }

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

            // Auto-export CSV if enabled
            if (trackerService.AutoExportCsv.Value)
            {
                trackerService.ExportSessionsToCsv();
            }

            // Prune old sessions based on retention limit
            trackerService.StorageService?.PruneOldSessions(trackerService.ArchiveRetention.Value);

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
