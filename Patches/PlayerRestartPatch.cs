using osu.Game.Screens.Play;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens.Patches
{
    /// <summary>
    /// Hooks Player.Restart (quick retry Ctrl+R or retry from pause menu) to capture unpassed retry attempts.
    /// </summary>
    public sealed class PlayerRestartPatch : PluginPatch<LazerLensPlugin>
    {
        public PlayerRestartPatch(LazerLensPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(Player), "Restart", MethodType.Prefix)
        {
        }

        public void Prefix(Player __instance)
        {
            if (__instance != null)
                Plugin.RecordUnpassedPlayerScore(__instance);
        }
    }
}