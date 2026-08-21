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
        /// Checks whether the Player instance has failed (HP = 0 or FailOverlay triggered).
        /// </summary>
        public static bool IsPlayerFailed(Player player)
        {
            try
            {
                var hpProp = typeof(Player).GetProperty("HealthProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var hp = hpProp?.GetValue(player) as HealthProcessor;
                if (hp?.HasFailed == true)
                    return true;

                var failOverlayProp = typeof(Player).GetProperty("FailOverlay", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (failOverlayProp?.GetValue(player) is FailOverlay failOverlay &&
                    failOverlay.State.Value == osu.Framework.Graphics.Containers.Visibility.Visible)
                {
                    return true;
                }
            }
            catch
            {
                // Defensive fallback: assume not failed if reflection fails
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
                var spProp = typeof(Player).GetProperty("ScoreProcessor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var sp = spProp?.GetValue(player) as ScoreProcessor;
                sp?.PopulateScore(scoreInfo);
            }
            catch
            {
                // Non-critical: manual score info will be used if processor population fails
            }
        }
    }
}
