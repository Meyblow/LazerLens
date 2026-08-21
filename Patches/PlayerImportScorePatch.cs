using System;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens.Patches
{
    /// <summary>
    /// Hooks Player.ImportScore to record completed (passed) plays.
    /// </summary>
    public sealed class PlayerImportScorePatch : PluginPatch<LazerLensPlugin>
    {
        public PlayerImportScorePatch(LazerLensPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(Player), "ImportScore", MethodType.Postfix)
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

            Plugin.OnScoreImported(score.ScoreInfo, true);
        }
    }
}