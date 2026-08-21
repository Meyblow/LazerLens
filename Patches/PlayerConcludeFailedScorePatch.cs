using System;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens.Patches
{
    /// <summary>
    /// Hooks Player.ConcludeFailedScore to capture native failed plays with complete score details.
    /// </summary>
    public sealed class PlayerConcludeFailedScorePatch : PluginPatch<LazerLensPlugin>
    {
        public PlayerConcludeFailedScorePatch(LazerLensPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(Player), "ConcludeFailedScore", MethodType.Postfix)
        {
        }

        public void Postfix(Player __instance, Score score)
        {
            if (__instance == null || score?.ScoreInfo == null) return;

            string typeName = __instance.GetType().Name;
            if (typeName.Contains("Replay") || typeName.Contains("Spectator"))
                return;

            if (!LazerLensPlugin.TryMarkPlayerRecorded(__instance))
                return;

            if (score.ScoreInfo.Date == default)
                score.ScoreInfo.Date = DateTimeOffset.Now;

            score.ScoreInfo.Rank = ScoreRank.F;
            Plugin.OnScoreImported(score.ScoreInfo, false);
        }
    }
}