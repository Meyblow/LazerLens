using osu.Game.Screens.Ranking;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens.Patches
{
    /// <summary>
    /// Hooks ResultsScreen.LoadComplete to receive updated score statistics and calculated PP.
    /// </summary>
    public sealed class ResultsScreenLoadCompletePatch : PluginPatch<LazerLensPlugin>
    {
        public ResultsScreenLoadCompletePatch(LazerLensPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(ResultsScreen), "LoadComplete", MethodType.Postfix)
        {
        }

        public void Postfix(ResultsScreen __instance)
        {
            var finalScore = __instance.Score;
            if (finalScore != null)
                Plugin.OnScoreUpdated(finalScore);

            Plugin.CheckStatsOnResults();
        }
    }
}