using System;
using System.Linq;
using System.Reflection;
using osu.Game.Online;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osucc.Client;

namespace LazerLens.Utilities
{
    /// <summary>
    /// Centralized reflection helper for accessing internal osu! game components safely (§12 of style-guide).
    /// All reflection lookups are isolated here with defensive exception handling and caching.
    /// </summary>
    public static class ReflectionHelper
    {
        private static UserStatisticsWatcher? cachedWatcher;
        private static LocalUserStatisticsProvider? cachedStatsProvider;

        /// <summary>
        /// Retrieves the active UserStatisticsWatcher from the OsuGame instance.
        /// Searches private fields across the type hierarchy with a DI fallback.
        /// </summary>
        public static UserStatisticsWatcher? GetUserStatisticsWatcher()
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

        /// <summary>
        /// Retrieves the LocalUserStatisticsProvider from OsuGame for tracking real-time profile PP deltas.
        /// </summary>
        public static LocalUserStatisticsProvider? GetLocalUserStatisticsProvider()
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

        /// <summary>
        /// Checks whether the Player instance has failed (HP = 0, HasFailed bindable, or FailOverlay triggered).
        /// </summary>
        public static bool IsPlayerFailed(Player player)
        {
            try
            {
                if (player == null) return false;

                if (player.GameplayState?.HasPassed == true)
                    return false;

                if (player.GameplayState?.Score?.ScoreInfo?.Rank == ScoreRank.F)
                    return true;

                for (Type? t = player.GetType(); t != null; t = t.BaseType)
                {
                    var hpProp = t.GetProperty("HealthProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (hpProp?.GetValue(player) is HealthProcessor hp)
                    {
                        if (hp.HasFailed || hp.Health.Value <= 0.0001)
                            return true;
                    }

                    var hpField = t.GetField("healthProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                               ?? t.GetField("_healthProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (hpField?.GetValue(player) is HealthProcessor hpF)
                    {
                        if (hpF.HasFailed || hpF.Health.Value <= 0.0001)
                            return true;
                    }

                    var foField = t.GetField("failOverlay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                               ?? t.GetField("_failOverlay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (foField?.GetValue(player) is FailOverlay fo && fo.State.Value == osu.Framework.Graphics.Containers.Visibility.Visible)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Defensive fallback
            }

            return false;
        }

        /// <summary>
        /// Populates score information from the player's internal ScoreProcessor.
        /// </summary>
        public static void TryPopulateScoreProcessor(Player player, ScoreInfo scoreInfo)
        {
            try
            {
                if (player == null || scoreInfo == null) return;

                for (Type? t = player.GetType(); t != null; t = t.BaseType)
                {
                    var spProp = t.GetProperty("ScoreProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (spProp?.GetValue(player) is ScoreProcessor sp)
                    {
                        sp.PopulateScore(scoreInfo);
                        return;
                    }

                    var spField = t.GetField("scoreProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                               ?? t.GetField("_scoreProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (spField?.GetValue(player) is ScoreProcessor spF)
                    {
                        spF.PopulateScore(scoreInfo);
                        return;
                    }
                }

                if (player.GameplayState?.Score?.ScoreInfo != null)
                {
                    var stateInfo = player.GameplayState.Score.ScoreInfo;
                    if (scoreInfo.TotalScore == 0 && stateInfo.TotalScore > 0)
                        scoreInfo.TotalScore = stateInfo.TotalScore;
                    if (scoreInfo.Accuracy == 0 && stateInfo.Accuracy > 0)
                        scoreInfo.Accuracy = stateInfo.Accuracy;
                    if (scoreInfo.MaxCombo == 0 && stateInfo.MaxCombo > 0)
                        scoreInfo.MaxCombo = stateInfo.MaxCombo;
                    if (scoreInfo.Statistics == null || scoreInfo.Statistics.Count == 0)
                        scoreInfo.Statistics = stateInfo.Statistics;
                }
            }
            catch
            {
                // Non-critical: manual score info will be used if processor population fails
            }
        }
    }
}
