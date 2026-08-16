using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Online;
using osu.Game.Rulesets.Mods;
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
        public SessionState ViewedState => _viewedState ?? LiveState;

        /// <summary>Shortcut kept for backward compat — points to ViewedState.</summary>
        public SessionState State => ViewedState;

        /// <summary>Whether the overlay is showing an archived session instead of the live one.</summary>
        public bool IsViewingArchive => _viewedState != null;

        public event Action? OnSessionUpdated;
        public event Action<SessionPlayRecord, bool>? OnNewPlayRecorded;

        public Bindable<bool> TrackRetries { get; } = new(true);
        public Bindable<bool> NotifyOnPlay { get; } = new(true);
        public Bindable<bool> CelebrateBest { get; } = new(true);
        public Bindable<bool> CompactMode { get; } = new(false);
        public Bindable<bool> ShowUR { get; } = new(true);

        private SessionState? _viewedState;
        private SessionStorageService? _storageService;

        public void OpenSessionsDirectory()
        {
            _storageService?.OpenSessionsDirectory();
        }

        public string? ExportSessionsToCsv()
        {
            return _storageService?.ExportToCsv();
        }

        public void RecordScore(ScoreInfo score, bool passed)
        {
            LazerLensPatch.DebugLog($"LazerLensService.RecordScore: entered. Title={score.BeatmapInfo?.Metadata?.Title}, Passed={passed}");

            // Prevent duplicate recording of the same play (e.g. from both Player.ImportScore and ResultsScreen.LoadComplete)
            var lastPlay = LiveState.Plays.LastOrDefault();
            if (lastPlay != null && lastPlay.Passed && passed &&
                lastPlay.OnlineBeatmapID == (score.BeatmapInfo?.OnlineID ?? 0) &&
                lastPlay.TotalScore == score.TotalScore &&
                lastPlay.MaxCombo == score.MaxCombo &&
                (DateTimeOffset.Now - lastPlay.Timestamp).TotalSeconds < 30)
            {
                LazerLensPatch.DebugLog("LazerLensService.RecordScore: duplicate pass detected, updating instead of adding.");
                UpdateScore(score); // Just update it with any new stats (like PP)
                return;
            }

            var previousBest = LiveState.BestScore;

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

            double? rawPp = score.PP ?? 0.0;
            double? ur = TryCalculateUR(score);

            var statsDict = score.Statistics != null
                ? new Dictionary<osu.Game.Rulesets.Scoring.HitResult, int>(score.Statistics)
                : new Dictionary<osu.Game.Rulesets.Scoring.HitResult, int>();

            var record = new SessionPlayRecord(
                BeatmapTitle: score.BeatmapInfo?.Metadata?.Title ?? "Unknown Title",
                BeatmapArtist: score.BeatmapInfo?.Metadata?.Artist ?? "Unknown Artist",
                DifficultyName: score.BeatmapInfo?.DifficultyName ?? "Normal",
                RulesetName: score.Ruleset?.Name ?? "osu!",
                Accuracy: Math.Truncate(score.Accuracy * 10000.0) / 100.0,
                TotalScore: score.TotalScore,
                MaxCombo: score.MaxCombo,
                Grade: passed ? score.Rank.ToString() : "F",
                Mods: score.Mods?.Select(m => m.Acronym).ToArray() ?? Array.Empty<string>(),
                Passed: passed,
                Timestamp: DateTimeOffset.Now,
                StarRating: score.BeatmapInfo?.StarRating ?? 0.0,
                PerformancePoints: rawPp,
                ProfilePerformancePoints: 0.0,
                Status: statusStr,
                OnlineBeatmapID: score.BeatmapInfo?.OnlineID ?? 0,
                OnlineBeatmapSetID: score.BeatmapInfo?.BeatmapSet?.OnlineID ?? 0,
                Rank: passed ? score.Rank : ScoreRank.F,
                Statistics: statsDict,
                UnstableRate: ur
            );

            LiveState.Plays.Add(record);
            LazerLensPatch.DebugLog($"LazerLensService.RecordScore: Play added! Total plays in LiveState: {LiveState.Plays.Count}. Firing OnSessionUpdated.");
            OnSessionUpdated?.Invoke();
            AutoSave();

            // If raw PP is not populated yet or 0 on a pass, compute it asynchronously
            if (passed && (!score.PP.HasValue || score.PP.Value <= 0))
            {
                calculateAndAssignPP(score, record, previousBest);
            }
            else
            {
                triggerNewPlayEvent(record, previousBest);
            }
        }

        private void triggerNewPlayEvent(SessionPlayRecord record, SessionPlayRecord? previousBest)
        {
            if (!record.Passed) return;

            bool isNewBest = previousBest == null ||
                (record.PerformancePoints ?? 0) > (previousBest.PerformancePoints ?? 0) ||
                ((record.PerformancePoints ?? 0) == (previousBest.PerformancePoints ?? 0) && record.TotalScore > previousBest.TotalScore);

            OnNewPlayRecorded?.Invoke(record, isNewBest);
        }

        private void calculateAndAssignPP(ScoreInfo score, SessionPlayRecord record, SessionPlayRecord? previousBest)
        {
            Task.Run(async () =>
            {
                try
                {
                    double? calculatedPp = await CalculatePerformanceAsync(score);
                    if (calculatedPp.HasValue && calculatedPp.Value > 0)
                    {
                        record = record with { PerformancePoints = calculatedPp.Value };
                        UpdatePlay(record.Id, p => p with { PerformancePoints = calculatedPp.Value });
                    }
                }
                catch { /* Silently ignore — PP calc can fail for custom rulesets */ }
                finally
                {
                    triggerNewPlayEvent(record, previousBest);
                }
            });
        }

        public void OnUserStatisticsUpdated(ScoreBasedUserStatisticsUpdate update)
        {
            if (update?.Before == null || update.After == null)
                return;

            decimal ppBefore = update.Before.PP ?? 0;
            decimal ppAfter = update.After.PP ?? 0;
            double roundedDelta = Math.Round((double)(ppAfter - ppBefore));

            // Find matching play in current session
            SessionPlayRecord? match = null;

            if (update.Score != null)
            {
                match = LiveState.Plays.LastOrDefault(p =>
                    (update.Score.BeatmapInfo?.OnlineID > 0 && p.OnlineBeatmapID == update.Score.BeatmapInfo.OnlineID && p.TotalScore == update.Score.TotalScore) ||
                    p.TotalScore == update.Score.TotalScore
                );
            }

            match ??= LiveState.Plays.LastOrDefault(p => p.Passed);

            if (match != null)
                UpdatePlay(match.Id, p => p with { ProfilePerformancePoints = roundedDelta }, save: true);
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
            long totalScore = score.TotalScore;

            var matching = LiveState.Plays.LastOrDefault(p =>
                (title.Length > 0 && p.BeatmapTitle == title && p.TotalScore == totalScore) ||
                p.TotalScore == totalScore) ?? LiveState.Plays.LastOrDefault();

            if (matching == null) return;

            double? ur = matching.UnstableRate ?? TryCalculateUR(score);
            double? rawPp = (score.PP.HasValue && score.PP.Value > 0) ? score.PP.Value : matching.PerformancePoints;
            var statsDict = score.Statistics != null && score.Statistics.Count > 0
                ? new Dictionary<osu.Game.Rulesets.Scoring.HitResult, int>(score.Statistics)
                : matching.Statistics;

            UpdatePlay(matching.Id, p => p with
            {
                PerformancePoints = rawPp,
                UnstableRate = ur,
                Statistics = statsDict
            }, save: true);
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
                var urResult = osu.Game.Rulesets.Scoring.HitEventExtensions.CalculateUnstableRate(score.HitEvents);
                return urResult.Result;
            }
            catch
            {
                return null; // Silently ignore — some rulesets don't support UR
            }
        }

        // --- PP calculation ---

        public static async Task<double?> CalculatePerformanceAsync(ScoreInfo score)
        {
            if (score.PP.HasValue && score.PP.Value > 0)
                return score.PP.Value;

            if (score.BeatmapInfo == null || score.Ruleset == null)
                return null;

            var diffCache = ClientApi.Game?.Dependencies?.Get(typeof(BeatmapDifficultyCache)) as BeatmapDifficultyCache;
            if (diffCache == null)
                return null;

            var starDiff = await diffCache.GetDifficultyAsync(score.BeatmapInfo, score.Ruleset, score.Mods);
            if (starDiff == null || starDiff.Value.DifficultyAttributes == null)
                return null;

            var ruleset = score.Ruleset.CreateInstance();
            var perfCalc = ruleset.CreatePerformanceCalculator();
            if (perfCalc == null)
                return null;

            var perfAttributes = perfCalc.Calculate(score, starDiff.Value.DifficultyAttributes);
            return perfAttributes.Total;
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

        public void AttachStorage(osu.Framework.Platform.Storage? storage)
        {
            _storageService = new SessionStorageService(storage);
        }

        public SessionStorageService? StorageService => _storageService;

        public void AutoSave()
        {
            if (_storageService == null || LiveState.Plays.Count == 0) return;

            try
            {
                _storageService.SaveSession(LiveState);
            }
            catch { /* Silently ignore — don't crash the game over a failed save */ }
        }

        public void SelectSession(Guid? sessionId)
        {
            if (sessionId == null)
            {
                _viewedState = null;
            }
            else
            {
                _viewedState = _storageService?.LoadSession(sessionId.Value);
            }

            OnSessionUpdated?.Invoke();
        }

        public void ReturnToLive()
        {
            _viewedState = null;
            OnSessionUpdated?.Invoke();
        }

        public List<SessionSummary> GetAllSessionSummaries()
        {
            return _storageService?.GetAllSessions() ?? new List<SessionSummary>();
        }

        public void ResetSession()
        {
            // Save current session before resetting (if it has plays)
            AutoSave();

            LiveState.Reset();
            _viewedState = null;
            OnSessionUpdated?.Invoke();
        }
    }
}
