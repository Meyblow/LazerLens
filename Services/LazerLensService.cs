using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Online;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Users;
using osucc.Client;
using LazerLens.Models;

namespace LazerLens.Services
{
    public class LazerLensService
    {
        /// <summary>The ongoing live session receiving gameplay events.</summary>
        public SessionState LiveState { get; } = new();

        /// <summary>The session currently displayed in the overlay (Live or Archived).</summary>
        public SessionState ViewedState => viewedState ?? LiveState;

        /// <summary>Shortcut kept for backward compat — points to ViewedState.</summary>
        public SessionState State => ViewedState;

        /// <summary>Whether the overlay is showing an archived session instead of the live one.</summary>
        public bool IsViewingArchive => viewedState != null;

        public event Action? OnSessionUpdated;
        public event Action<SessionPlayRecord>? OnNewPlayRecorded;
        public event Action<SessionGoal>? OnGoalAchieved;
        public event Action? OnSessionReset;

        // 1. Metrics & Display
        public Bindable<DefaultSortMode> DefaultSort { get; } = new(DefaultSortMode.TimeDesc);
        public Bindable<PpDisplayMode> PpDisplay { get; } = new(PpDisplayMode.Both);
        public Bindable<AccuracyCalculationMode> AccuracyCalculation { get; } = new(AccuracyCalculationMode.ObjectWeighted);
        public Bindable<bool> HighlightUR { get; } = new(true);
        public Bindable<bool> ShowModsInHistory { get; } = new(true);
        public Bindable<bool> ShowDifficultyRating { get; } = new(true);

        // 2. Session Management
        public Bindable<SessionSplitThreshold> SessionSplit { get; } = new(SessionSplitThreshold.Midnight);
        public Bindable<AfkPauseTimeout> AfkPause { get; } = new(AfkPauseTimeout.FiveMinutes);
        public Bindable<bool> EnableSessionPause { get; } = new(false);
        public Bindable<bool> IsSessionPaused { get; } = new(false);
        public Bindable<bool> AutoExportCsv { get; } = new(false);
        public Bindable<ArchiveRetentionLimit> ArchiveRetention { get; } = new(ArchiveRetentionLimit.Unlimited);

        // 3. Recording Filters
        public Bindable<int> MinPlayDurationSeconds { get; } = new(5);
        public Bindable<bool> TrackStandard { get; } = new(true);
        public Bindable<bool> TrackTaiko { get; } = new(true);
        public Bindable<bool> TrackCatch { get; } = new(true);
        public Bindable<bool> TrackMania { get; } = new(true);
        public Bindable<bool> TrackCustomRulesets { get; } = new(true);
        public Bindable<bool> IgnoreNoFailPlays { get; } = new(false);
        public Bindable<bool> RankedLovedOnly { get; } = new(false);

        // 4. Notifications
        public Bindable<PlayNotificationFilter> PlayNotifFilter { get; } = new(PlayNotificationFilter.PassedOnly);
        public Bindable<bool> NotifySessionBest { get; } = new(true);
        public Bindable<MilestoneNotificationMode> Milestones { get; } = new(MilestoneNotificationMode.FiftyPlays);

        // 5. Overlay & Toolbar
        public Bindable<bool> AutoOpenOverlayOnPass { get; } = new(false);
        public Bindable<ToolbarBadgeMode> ToolbarBadge { get; } = new(ToolbarBadgeMode.PlayCount);
        public Bindable<string> ToolbarBadgeColor { get; } = new("#00d2ff");
        public Bindable<SearchBarPosition> SearchPosition { get; } = new(SearchBarPosition.Right);
        public Bindable<int> OverlayWidth { get; } = new(960);
        public Bindable<float> OverlayBackdropOpacity { get; } = new(0.9f);

        // 6. v1.6.0 Goals, Warmup & Graph
        public Bindable<bool> IsWarmupMode { get; } = new(false);
        public Bindable<SessionGoal?> ActiveGoal { get; } = new();
        public Bindable<bool> ShowSessionGraph { get; } = new(true);
        public Bindable<bool> ExcludeWarmupFromStats { get; } = new(false);
        public Bindable<ShareFormattingMode> ShareFormatting { get; } = new(ShareFormattingMode.Markdown);

        // Existing / Backward Compatibility
        public Bindable<bool> TrackRetries { get; } = new(false);
        public Bindable<bool> NotifyOnPlay { get; } = new(true);
        public Bindable<bool> CompactMode { get; } = new(true);
        public Bindable<bool> ShowUR { get; } = new(true);

        private SessionState? viewedState;
        public SessionStorageService? StorageService { get; private set; }

        public LazerLensService()
        {
            ActiveGoal.BindValueChanged(_ => checkGoalProgress());
        }

        public void OpenSessionsDirectory()
        {
            StorageService?.OpenSessionsDirectory();
        }

        public void OpenSessionFile(Guid sessionId)
        {
            StorageService?.OpenSessionFile(sessionId);
        }

        public string? ExportSessionsToCsv()
        {
            return StorageService?.ExportToCsv();
        }

        public void ResetLiveSession()
        {
            AutoSave();
            LiveState.Reset();
            viewedState = null;
            OnSessionReset?.Invoke();
            OnSessionUpdated?.Invoke();
        }

        public void RecordScore(ScoreInfo score, bool passed)
        {
            if (score == null)
                return;

            if (IsSessionPaused.Value)
                return;

            checkSessionSplit();

            // Play duration filter
            double playDurationSeconds = 0;
            if (score.HitEvents != null && score.HitEvents.Count > 1)
            {
                var objects = score.HitEvents.Select(e => e.HitObject).Where(o => o != null).ToList();
                if (objects.Count > 1)
                {
                    double firstHit = objects.Min(o => o!.StartTime);
                    double lastHit = objects.Max(o => o!.StartTime);
                    playDurationSeconds = (lastHit - firstHit) / 1000.0;
                }
            }

            if (playDurationSeconds > 0 && playDurationSeconds < MinPlayDurationSeconds.Value)
            {
                return;
            }

            // Ruleset tracking filter
            string rulesetName = score.Ruleset?.ShortName ?? score.Ruleset?.Name ?? "osu";
            bool isTaiko = rulesetName.Contains("taiko", StringComparison.OrdinalIgnoreCase);
            bool isCatch = rulesetName.Contains("catch", StringComparison.OrdinalIgnoreCase) || rulesetName.Contains("fruit", StringComparison.OrdinalIgnoreCase);
            bool isMania = rulesetName.Contains("mania", StringComparison.OrdinalIgnoreCase);
            bool isStandard = rulesetName.Equals("osu", StringComparison.OrdinalIgnoreCase) || (!isTaiko && !isCatch && !isMania && (rulesetName.Contains("standard", StringComparison.OrdinalIgnoreCase) || rulesetName.Equals("osu!", StringComparison.OrdinalIgnoreCase)));
            bool isCustom = !isStandard && !isTaiko && !isCatch && !isMania;

            if (isStandard && !TrackStandard.Value) return;
            if (isTaiko && !TrackTaiko.Value) return;
            if (isCatch && !TrackCatch.Value) return;
            if (isMania && !TrackMania.Value) return;
            if (isCustom && !TrackCustomRulesets.Value) return;

            // Ignore NoFail plays if configured
            if (IgnoreNoFailPlays.Value && score.Mods != null && score.Mods.Any(m => m.Acronym.Equals("NF", StringComparison.OrdinalIgnoreCase)))
                return;

            // Prevent duplicate recording of the same play
            var lastPlay = LiveState.Plays.LastOrDefault();
            if (lastPlay != null && lastPlay.Passed && passed &&
                lastPlay.OnlineBeatmapID == (score.BeatmapInfo?.OnlineID ?? 0) &&
                lastPlay.TotalScore == score.TotalScore &&
                lastPlay.MaxCombo == score.MaxCombo &&
                (DateTimeOffset.Now - lastPlay.Timestamp).TotalSeconds < 30)
            {
                UpdateScore(score);
                return;
            }

            string statusStr = score.BeatmapInfo?.Status switch
            {
                BeatmapOnlineStatus.Ranked => "Ranked",
                BeatmapOnlineStatus.Approved => "Approved",
                BeatmapOnlineStatus.Qualified => "Qualified",
                BeatmapOnlineStatus.Loved => "Loved",
                BeatmapOnlineStatus.Pending => "Pending",
                BeatmapOnlineStatus.WIP => "WIP",
                BeatmapOnlineStatus.Graveyard => "Graveyard",
                BeatmapOnlineStatus.LocallyModified => "Local",
                BeatmapOnlineStatus.None => "Local",
                _ => score.BeatmapInfo?.Status.ToString() ?? "Ranked"
            };

            if (RankedLovedOnly.Value && statusStr is not ("Ranked" or "Approved" or "Loved"))
                return;

            double? rawPp = score.PP ?? 0.0;
            double? ur = TryCalculateUR(score);

            var statsDict = score.Statistics != null
                ? new Dictionary<osu.Game.Rulesets.Scoring.HitResult, int>(score.Statistics)
                : new Dictionary<osu.Game.Rulesets.Scoring.HitResult, int>();

            string[] modAcronyms = score.Mods != null
                ? score.Mods.Select(m => m.Acronym).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray()
                : Array.Empty<string>();

            var record = new SessionPlayRecord(
                BeatmapTitle: score.BeatmapInfo?.Metadata?.Title ?? "Unknown Title",
                BeatmapArtist: score.BeatmapInfo?.Metadata?.Artist ?? "Unknown Artist",
                DifficultyName: score.BeatmapInfo?.DifficultyName ?? "Normal",
                RulesetName: score.Ruleset?.Name ?? "osu!",
                Accuracy: Math.Truncate(score.Accuracy * 10000.0) / 100.0,
                TotalScore: score.TotalScore,
                MaxCombo: score.MaxCombo,
                Grade: passed ? score.Rank.ToString() : "F",
                Mods: modAcronyms,
                Passed: passed,
                Timestamp: DateTimeOffset.Now,
                StarRating: score.BeatmapInfo?.StarRating ?? 0.0,
                PerformancePoints: rawPp,
                ProfilePerformancePoints: null,
                Status: statusStr,
                OnlineBeatmapID: score.BeatmapInfo?.OnlineID ?? 0,
                OnlineBeatmapSetID: score.BeatmapInfo?.BeatmapSet?.OnlineID ?? 0,
                Rank: passed ? score.Rank : ScoreRank.F,
                Statistics: statsDict,
                UnstableRate: ur,
                IsChoke: false,
                IsWarmup: IsWarmupMode.Value,
                BeatmapMapper: score.BeatmapInfo?.Metadata?.Author?.Username ?? score.BeatmapInfo?.Metadata?.Author?.ToString() ?? string.Empty
            );

            LiveState.Plays.Add(record);
            OnSessionUpdated?.Invoke();
            AutoSave();

            // If raw PP is not populated yet or 0 on a pass, compute it asynchronously
            if (passed)
            {
                calculateAndAssignPP(score, record);
            }
            else
            {
                triggerNewPlayEvent(record);
            }

            checkGoalProgress();
        }

        private void checkGoalProgress()
        {
            var goal = ActiveGoal.Value;
            if (goal == null || goal.Type == SessionGoalType.None || goal.IsAchieved)
                return;

            if (goal.CheckAchieved(LiveState))
            {
                OnGoalAchieved?.Invoke(goal);
            }
        }

        private void triggerNewPlayEvent(SessionPlayRecord record)
        {
            if (PlayNotifFilter.Value == PlayNotificationFilter.Disabled)
                return;

            if (PlayNotifFilter.Value == PlayNotificationFilter.PassedOnly && !record.Passed)
                return;

            if (PlayNotifFilter.Value == PlayNotificationFilter.SessionBestsOnly)
            {
                var best = LiveState.BestScore;
                if (best == null || best.Id != record.Id)
                    return;
            }

            OnNewPlayRecorded?.Invoke(record);
        }

        private void calculateAndAssignPP(ScoreInfo score, SessionPlayRecord record)
        {
            // Calculate performance asynchronously to prevent UI hitches on map completion
            Task.Run(async () =>
            {
                try
                {
                    double? calculatedPp = await CalculatePerformanceAsync(score);
                    double? ifFcPp = await CalculateIfFcPerformanceAsync(score);

                    double actualPp = calculatedPp ?? record.PerformancePoints ?? 0;
                    bool isChoke = false;
                    if (ifFcPp.HasValue && ifFcPp.Value > actualPp * 1.08 && record.CountMiss <= 3 && actualPp > 0)
                    {
                        isChoke = true;
                    }

                    if (LazerLensPlugin.Instance?.Host.Scheduler != null)
                    {
                        LazerLensPlugin.Instance.Host.Scheduler.Add(() =>
                        {
                            UpdatePlay(record.Id, p => p with
                            {
                                PerformancePoints = calculatedPp ?? p.PerformancePoints,
                                IfFcPerformancePoints = ifFcPp,
                                IsChoke = isChoke
                            });

                            var updated = LiveState.Plays.FirstOrDefault(p => p.Id == record.Id) ?? record;
                            triggerNewPlayEvent(updated);
                            checkGoalProgress();
                        });
                    }
                    else
                    {
                        UpdatePlay(record.Id, p => p with
                        {
                            PerformancePoints = calculatedPp ?? p.PerformancePoints,
                            IfFcPerformancePoints = ifFcPp,
                            IsChoke = isChoke
                        });

                        var updated = LiveState.Plays.FirstOrDefault(p => p.Id == record.Id) ?? record;
                        triggerNewPlayEvent(updated);
                        checkGoalProgress();
                    }
                    return;
                }
                catch { /* PP calculation can fail for unconverted or custom rulesets; safely ignored */ }

                if (LazerLensPlugin.Instance?.Host.Scheduler != null)
                    LazerLensPlugin.Instance.Host.Scheduler.Add(() => triggerNewPlayEvent(record));
                else
                    triggerNewPlayEvent(record);
            });
        }

        public void OnUserStatisticsUpdated(ScoreBasedUserStatisticsUpdate update)
        {
            if (update?.Before == null || update.After == null)
                return;

            decimal ppBefore = update.Before.PP ?? 0;
            decimal ppAfter = update.After.PP ?? 0;
            double roundedDelta = Math.Round((double)(ppAfter - ppBefore));

            // Find matching play in current session with ruleset awareness
            SessionPlayRecord? match = null;

            if (update.Score != null)
            {
                string updateRuleset = update.Score.Ruleset?.ShortName ?? update.Score.Ruleset?.Name ?? "";

                match = LiveState.Plays.LastOrDefault(p =>
                    (update.Score.BeatmapInfo?.OnlineID > 0 && p.OnlineBeatmapID == update.Score.BeatmapInfo.OnlineID && p.TotalScore == update.Score.TotalScore) ||
                    (!string.IsNullOrEmpty(updateRuleset) && string.Equals(p.RulesetName, updateRuleset, StringComparison.OrdinalIgnoreCase) && p.TotalScore == update.Score.TotalScore) ||
                    p.TotalScore == update.Score.TotalScore
                );
            }

            match ??= LiveState.Plays.LastOrDefault(p => p.Passed);

            if (match != null)
            {
                UpdatePlay(match.Id, p => p with { ProfilePerformancePoints = roundedDelta }, save: true);
            }
        }

        public void OnDirectStatisticsUpdated(UserStatistics oldStats, UserStatistics newStats)
        {
            if (oldStats == null || newStats == null)
                return;

            decimal ppOld = oldStats.PP ?? 0;
            decimal ppNew = newStats.PP ?? 0;
            double delta = Math.Round((double)(ppNew - ppOld));

            var lastPlay = LiveState.Plays.LastOrDefault(p => p.Passed);
            if (lastPlay != null)
                UpdatePlay(lastPlay.Id, p => p with { ProfilePerformancePoints = delta }, save: true);
        }

        public void UpdateScore(ScoreInfo score)
        {
            if (score == null)
                return;

            string title = score.BeatmapInfo?.Metadata?.Title ?? "";
            int onlineId = score.BeatmapInfo?.OnlineID ?? 0;
            long totalScore = score.TotalScore;

            var matching = LiveState.Plays.LastOrDefault(p =>
                (onlineId > 0 && p.OnlineBeatmapID == onlineId && (DateTimeOffset.Now - p.Timestamp).TotalMinutes < 10) ||
                (title.Length > 0 && p.BeatmapTitle == title && p.TotalScore == totalScore) ||
                (title.Length > 0 && p.BeatmapTitle == title && (DateTimeOffset.Now - p.Timestamp).TotalMinutes < 10));

            if (matching == null) return;

            double? ur = matching.UnstableRate ?? TryCalculateUR(score);
            double? rawPp = (score.PP.HasValue && score.PP.Value > 0) ? score.PP.Value : matching.PerformancePoints;
            var statsDict = score.Statistics != null && score.Statistics.Count > 0
                ? new Dictionary<osu.Game.Rulesets.Scoring.HitResult, int>(score.Statistics)
                : matching.Statistics;

            string[] modAcronyms = score.Mods != null && score.Mods.Length > 0
                ? score.Mods.Select(m => m.Acronym).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray()
                : matching.Mods;

            UpdatePlay(matching.Id, p => p with
            {
                PerformancePoints = rawPp,
                UnstableRate = ur,
                Statistics = statsDict,
                TotalScore = totalScore > 0 ? totalScore : p.TotalScore,
                Mods = modAcronyms
            }, save: true);

            // If raw PP is still null/0 on a pass, try computing it now that full statistics are available
            if (matching.Passed && (!rawPp.HasValue || rawPp.Value <= 0))
            {
                calculateAndAssignPP(score, matching);
            }
        }

        private void checkSessionSplit()
        {
            if (LiveState.Plays.Count == 0) return;

            var lastPlay = LiveState.Plays.LastOrDefault();
            if (lastPlay == null) return;

            bool shouldSplit = false;
            var now = DateTimeOffset.Now;

            switch (SessionSplit.Value)
            {
                case SessionSplitThreshold.Midnight:
                    if (now.Date > lastPlay.Timestamp.Date)
                        shouldSplit = true;
                    break;

                case SessionSplitThreshold.TwoHours:
                    if ((now - lastPlay.Timestamp).TotalHours >= 2)
                        shouldSplit = true;
                    break;

                case SessionSplitThreshold.FourHours:
                    if ((now - lastPlay.Timestamp).TotalHours >= 4)
                        shouldSplit = true;
                    break;
            }

            if (shouldSplit)
            {
                ResetLiveSession();
            }
        }

        // --- Shared play update helper ---

        /// <summary>
        /// Finds a play by ID in LiveState and applies a transformation to it.
        /// Fires OnSessionUpdated and optionally AutoSaves.
        /// </summary>
        private void UpdatePlay(Guid id, Func<SessionPlayRecord, SessionPlayRecord> transform, bool save = false)
        {
            for (int i = 0; i < LiveState.Plays.Count; i++)
            {
                if (LiveState.Plays[i].Id == id)
                {
                    LiveState.Plays[i] = transform(LiveState.Plays[i]);
                    checkGoalProgress();
                    OnSessionUpdated?.Invoke();
                    if (save) AutoSave();
                    return;
                }
            }
        }

        // --- UR calculation helper ---

        private static double? TryCalculateUR(ScoreInfo score)
        {
            if (score?.HitEvents == null || score.HitEvents.Count == 0)
                return null;

            try
            {
                var hitErrors = score.HitEvents
                    .Select(e => e.TimeOffset)
                    .ToList();

                if (hitErrors.Count == 0) return null;

                double mean = hitErrors.Average();
                double sumSquares = hitErrors.Sum(e => (e - mean) * (e - mean));
                double stdDev = Math.Sqrt(sumSquares / hitErrors.Count);
                return stdDev * 10.0;
            }
            catch
            {
                return null;
            }
        }

        // --- PP calculation ---

        public static async Task<double?> CalculatePerformanceAsync(ScoreInfo score)
        {
            if (score.PP.HasValue && score.PP.Value > 0)
                return score.PP.Value;

            if (score.BeatmapInfo == null || score.Ruleset == null)
                return null;

            try
            {
                var ruleset = score.Ruleset.CreateInstance();
                var perfCalc = ruleset?.CreatePerformanceCalculator();
                if (perfCalc == null)
                    return null;

                // 1. Try BeatmapDifficultyCache
                var diffCache = ClientApi.Game?.Dependencies?.Get(typeof(BeatmapDifficultyCache)) as BeatmapDifficultyCache;
                if (diffCache != null)
                {
                    var starDiff = await diffCache.GetDifficultyAsync(score.BeatmapInfo, score.Ruleset, score.Mods);
                    if (starDiff?.DifficultyAttributes != null)
                    {
                        var perfAttributes = perfCalc.Calculate(score, starDiff.Value.DifficultyAttributes);
                        if (perfAttributes.Total > 0)
                            return perfAttributes.Total;
                    }
                }

                // 2. Direct fallback using BeatmapManager WorkingBeatmap
                var beatmapMgr = ClientApi.Game?.Dependencies?.Get(typeof(osu.Game.Beatmaps.BeatmapManager)) as osu.Game.Beatmaps.BeatmapManager;
                var working = beatmapMgr?.GetWorkingBeatmap(score.BeatmapInfo);
                if (working != null)
                {
                    var diffCalc = ruleset.CreateDifficultyCalculator(working);
                    var diffAttributes = diffCalc.Calculate(score.Mods);
                    if (diffAttributes != null)
                    {
                        var perfAttributes = perfCalc.Calculate(score, diffAttributes);
                        return perfAttributes.Total;
                    }
                }
            }
            catch
            {
                // Defensive fallback
            }

            return null;
        }

        public static async Task<double?> CalculateIfFcPerformanceAsync(ScoreInfo score)
        {
            if (score.BeatmapInfo == null || score.Ruleset == null)
                return null;

            try
            {
                var ruleset = score.Ruleset.CreateInstance();
                var perfCalc = ruleset?.CreatePerformanceCalculator();
                if (perfCalc == null)
                    return null;

                var fcScore = score.DeepClone();
                int misses = fcScore.Statistics.TryGetValue(HitResult.Miss, out int m) ? m : 0;
                int largeMisses = fcScore.Statistics.TryGetValue(HitResult.LargeTickMiss, out int lm) ? lm : 0;

                var stats = new Dictionary<HitResult, int>(fcScore.Statistics);
                stats[HitResult.Miss] = 0;
                stats[HitResult.LargeTickMiss] = 0;

                if (stats.ContainsKey(HitResult.Great))
                    stats[HitResult.Great] += (misses + largeMisses);
                else if (stats.ContainsKey(HitResult.Perfect))
                    stats[HitResult.Perfect] += (misses + largeMisses);

                fcScore.Statistics = stats;

                var diffCache = ClientApi.Game?.Dependencies?.Get(typeof(BeatmapDifficultyCache)) as BeatmapDifficultyCache;
                if (diffCache != null)
                {
                    var starDiff = await diffCache.GetDifficultyAsync(score.BeatmapInfo, score.Ruleset, score.Mods);
                    if (starDiff?.DifficultyAttributes != null)
                    {
                        fcScore.MaxCombo = starDiff.Value.DifficultyAttributes.MaxCombo;
                        var perfAttributes = perfCalc.Calculate(fcScore, starDiff.Value.DifficultyAttributes);
                        if (perfAttributes.Total > 0)
                            return perfAttributes.Total;
                    }
                }

                var beatmapMgr = ClientApi.Game?.Dependencies?.Get(typeof(osu.Game.Beatmaps.BeatmapManager)) as osu.Game.Beatmaps.BeatmapManager;
                var working = beatmapMgr?.GetWorkingBeatmap(score.BeatmapInfo);
                if (working != null)
                {
                    var diffCalc = ruleset.CreateDifficultyCalculator(working);
                    var diffAttributes = diffCalc.Calculate(score.Mods);
                    if (diffAttributes != null)
                    {
                        fcScore.MaxCombo = diffAttributes.MaxCombo;
                        var perfAttributes = perfCalc.Calculate(fcScore, diffAttributes);
                        return perfAttributes.Total;
                    }
                }
            }
            catch
            {
                // Defensive fallback
            }

            return null;
        }

        // --- Mod ranking check ---

        public static bool HasUnrankedMods(ScoreInfo score)
        {
            if (score.Mods == null || score.Mods.Length == 0)
                return false;

            foreach (var mod in score.Mods)
            {
                if (!mod.Ranked)
                    return true;

                string acr = mod.Acronym?.ToUpperInvariant() ?? "";
                if (acr is "RX" or "AP" or "AT" or "CN" or "DA" or "WU" or "WD" or "AS" or "TP" or "MR" or "SV2")
                    return true;

                if (mod.Type is ModType.Automation or ModType.System or ModType.Fun)
                    return true;
            }

            return false;
        }

        // --- Storage & Session Selection ---

        public void AttachStorage(osucc.Data.IOsuCcStorage? storage)
        {
            StorageService = new SessionStorageService(storage);
        }

        public void AutoSave()
        {
            if (StorageService == null || LiveState.Plays.Count == 0) return;

            try
            {
                StorageService.SaveSession(LiveState);
            }
            catch { /* Silently ignore — don't crash the game over a failed save */ }
        }

        public void SelectSession(Guid? sessionId)
        {
            if (sessionId == null)
            {
                viewedState = null;
            }
            else
            {
                viewedState = StorageService?.LoadSession(sessionId.Value);
            }

            OnSessionUpdated?.Invoke();
        }

        public void ReturnToLive()
        {
            viewedState = null;
            OnSessionUpdated?.Invoke();
        }

        public List<SessionSummary> GetAllSessionSummaries()
        {
            var sessions = StorageService?.GetAllSessions() ?? new List<SessionSummary>();
            return sessions.Where(s => s.Id != LiveState.Id).ToList();
        }

        public void ResetSession()
        {
            // Save current session before resetting (if it has plays)
            AutoSave();

            LiveState.Reset();
            viewedState = null;
            OnSessionUpdated?.Invoke();
        }
    }
}


