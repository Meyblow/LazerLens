using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Game.Rulesets.Mods;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Screens.Ranking;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens
{
    public static class LazerLensPatch
    {
        public static event Action<ScoreInfo, bool, bool>? OnScoreImported; // score, passed, isRetry
        public static event Action<ScoreInfo>? OnScoreUpdated;         // score with PP

        private static readonly HashSet<int> recordedPlayerHashes = new();

        public static void Install(IOsuCcPluginHost host)
        {
            var playerType = typeof(Player);

            // 1. Hook Player.ImportScore (Pass)
            var importMethod = playerType.GetMethod("ImportScore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (importMethod != null)
                host.AddPatch(importMethod, typeof(LazerLensPatch), nameof(ImportScorePostfix), MethodType.Postfix);

            // 2. Hook Player.ConcludeFailedScore (Fail — called natively on all failed plays with fully populated Score object)
            var concludeMethod = playerType.GetMethod("ConcludeFailedScore", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (concludeMethod != null)
                host.AddPatch(concludeMethod, typeof(LazerLensPatch), nameof(ConcludeFailedScorePostfix), MethodType.Postfix);

            // 3. Hook Player.Restart (Quick retry Ctrl+R or retry from pause screen)
            var restartMethod = playerType.GetMethod("Restart", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (restartMethod != null)
                host.AddPatch(restartMethod, typeof(LazerLensPatch), nameof(OnRestartPrefix), MethodType.Prefix);

            // 4. Hook Player.PerformExit (Quit via back button or Escape)
            var performExitMethod = playerType.GetMethod("PerformExit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (performExitMethod != null)
                host.AddPatch(performExitMethod, typeof(LazerLensPatch), nameof(OnPerformExitPrefix), MethodType.Prefix);

            // 5. Hook ResultsScreen.OnEntering (Catch computed PP & server stats)
            var resultsType = typeof(ResultsScreen);
            var onEnteringMethod = resultsType.GetMethod("OnEntering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (onEnteringMethod != null)
                host.AddPatch(onEnteringMethod, typeof(LazerLensPatch), nameof(ResultsOnEnteringPostfix), MethodType.Postfix);

            DebugLog($"LazerLensPatch installed: import={importMethod != null}, conclude={concludeMethod != null}, restart={restartMethod != null}, exit={performExitMethod != null}, results={onEnteringMethod != null}");
        }

        public static void DebugLog(string message)
        {
            try
            {
                string logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "lazerlens_debug.log");
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n");
            }
            catch { }
        }

        private static void ImportScorePostfix(Player __instance, Score score)
        {
            try
            {
                DebugLog($"ImportScorePostfix: instance={__instance?.GetType().Name}, score={score != null}");
                if (__instance == null || score?.ScoreInfo == null) return;
                
                string typeName = __instance.GetType().Name;
                if (typeName.Contains("Replay") || typeName.Contains("Spectator"))
                {
                    DebugLog($"ImportScorePostfix: Skipping because instance is {typeName}");
                    return;
                }

                lock (recordedPlayerHashes)
                {
                    int hash = __instance.GetHashCode();
                    if (recordedPlayerHashes.Contains(hash))
                    {
                        DebugLog($"ImportScorePostfix: hash {hash} already recorded, skipping duplicate.");
                        return;
                    }

                    if (recordedPlayerHashes.Count > 300)
                        recordedPlayerHashes.Clear();

                    recordedPlayerHashes.Add(hash);
                }

                DebugLog($"ImportScorePostfix: Dispatching PASS for {score.ScoreInfo.BeatmapInfo?.Metadata?.Title}");
                OnScoreImported?.Invoke(score.ScoreInfo, true, false);
            }
            catch (Exception ex)
            {
                DebugLog($"ImportScorePostfix error: {ex}");
            }
        }

        private static void ConcludeFailedScorePostfix(Player __instance, Score score)
        {
            try
            {
                DebugLog($"ConcludeFailedScorePostfix: instance={__instance?.GetType().Name}");
                if (__instance == null) return;
                
                string typeName = __instance.GetType().Name;
                if (typeName.Contains("Replay") || typeName.Contains("Spectator"))
                {
                    DebugLog($"ConcludeFailedScorePostfix: Skipping because instance is {typeName}");
                    return;
                }

                if (score?.ScoreInfo == null) return;

                lock (recordedPlayerHashes)
                {
                    int hash = __instance.GetHashCode();
                    if (recordedPlayerHashes.Contains(hash))
                    {
                        DebugLog($"ConcludeFailedScorePostfix: hash {hash} already recorded, skipping duplicate.");
                        return;
                    }

                    if (recordedPlayerHashes.Count > 300)
                        recordedPlayerHashes.Clear();

                    recordedPlayerHashes.Add(hash);
                }

                score.ScoreInfo.Rank = ScoreRank.F;
                DebugLog($"ConcludeFailedScorePostfix: Dispatching FAIL for {score.ScoreInfo.BeatmapInfo?.Metadata?.Title}, Acc={score.ScoreInfo.Accuracy:P2}, Combo={score.ScoreInfo.MaxCombo}");
                OnScoreImported?.Invoke(score.ScoreInfo, false, false);
            }
            catch (Exception ex)
            {
                DebugLog($"ConcludeFailedScorePostfix error: {ex}");
            }
        }

        private static void OnRestartPrefix(Player __instance)
        {
            DebugLog($"OnRestartPrefix: instance={__instance?.GetType().Name}");
            if (__instance != null)
                recordUnpassedScore(__instance, true);
        }

        private static void OnPerformExitPrefix(Player __instance)
        {
            DebugLog($"OnPerformExitPrefix: instance={__instance?.GetType().Name}");
            if (__instance != null)
                recordUnpassedScore(__instance, true);
        }

        private static void recordUnpassedScore(Player player, bool isRetry)
        {
            try
            {
                if (player == null) return;
                
                string typeName = player.GetType().Name;
                if (typeName.Contains("Replay") || typeName.Contains("Spectator"))
                {
                    DebugLog($"recordUnpassedScore: Skipping because instance is {typeName}");
                    return;
                }

                lock (recordedPlayerHashes)
                {
                    int hash = player.GetHashCode();
                    if (recordedPlayerHashes.Contains(hash))
                    {
                        DebugLog($"recordUnpassedScore: hash {hash} already recorded, skipping.");
                        return;
                    }

                    if (recordedPlayerHashes.Count > 300)
                        recordedPlayerHashes.Clear();

                    recordedPlayerHashes.Add(hash);
                }

                // If player already completed and passed, do not record as unpassed
                if (player.GameplayState?.HasPassed == true)
                {
                    DebugLog("recordUnpassedScore: GameplayState.HasPassed is true, skipping.");
                    return;
                }

                var scoreInfo = player.Score?.ScoreInfo ?? player.GameplayState?.Score?.ScoreInfo;
                if (scoreInfo == null)
                {
                    scoreInfo = new ScoreInfo();
                }

                // Assign metadata first so ScoreProcessor doesn't crash on null properties
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
                        scoreInfo.Mods = Array.Empty<Mod>();
                }

                // Populate live score data (accuracy, combo, statistics, total score) from score processor
                try
                {
                    var spProp = typeof(Player).GetProperty("ScoreProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var sp = spProp?.GetValue(player) as osu.Game.Rulesets.Scoring.ScoreProcessor;
                    sp?.PopulateScore(scoreInfo);
                }
                catch (Exception spEx)
                {
                    DebugLog($"recordUnpassedScore: ScoreProcessor populate error: {spEx}");
                }

                scoreInfo.Rank = ScoreRank.F;

                DebugLog($"recordUnpassedScore: Dispatching UNPASSED for {scoreInfo.BeatmapInfo?.Metadata?.Title}, Acc={scoreInfo.Accuracy:P2}, Combo={scoreInfo.MaxCombo}");
                bool hasFailed = player.GameplayState?.HasFailed == true;
                OnScoreImported?.Invoke(scoreInfo, false, hasFailed ? false : isRetry);
            }
            catch (Exception ex)
            {
                DebugLog($"recordUnpassedScore error: {ex}");
            }
        }

        private static void ResultsOnEnteringPostfix(ResultsScreen __instance)
        {
            try
            {
                DebugLog($"ResultsOnEnteringPostfix: instance={__instance?.GetType().Name}");
                var finalScore = __instance.Score ?? __instance.SelectedScore.Value;
                if (finalScore != null)
                {
                    // Always try to import the score from results screen in case ImportScore was skipped (e.g., Relax mod or unranked play)
                    // The service layer will handle deduplication
                    OnScoreImported?.Invoke(finalScore, true, false);
                }

                __instance.SelectedScore.BindValueChanged(e =>
                {
                    var score = e.NewValue ?? __instance.Score;
                    if (score != null)
                    {
                        OnScoreUpdated?.Invoke(score);
                    }
                }, true);

                LazerLensPlugin.Instance?.CheckStatsOnResults();
            }
            catch (Exception ex)
            {
                DebugLog($"ResultsOnEnteringPostfix error: {ex}");
            }
        }
    }
}

