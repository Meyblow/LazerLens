using osu.Game.Screens.Play;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens.Patches
{
    /// <summary>
    /// Hooks Player.PerformExit (quit via Escape or Back button) to capture unpassed quit attempts.
    /// </summary>
    public sealed class PlayerPerformExitPatch : PluginPatch<LazerLensPlugin>
    {
        public PlayerPerformExitPatch(LazerLensPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(Player), "PerformExit", MethodType.Prefix)
        {
        }

        public void Prefix(Player __instance)
        {
            if (__instance != null)
                Plugin.RecordUnpassedPlayerScore(__instance);
        }
    }
}