using System;
using osu.Game.Screens.Play;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens.Patches
{
    /// <summary>
    /// Hooks Player.PerformFail to capture failed plays immediately when fail occurs.
    /// </summary>
    public sealed class PlayerPerformFailPatch : PluginPatch<LazerLensPlugin>
    {
        public PlayerPerformFailPatch(LazerLensPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(Player), "PerformFail", MethodType.Postfix)
        {
        }

        public void Postfix(Player __instance)
        {
            if (__instance == null) return;

            string typeName = __instance.GetType().Name;
            if (typeName.Contains("Replay") || typeName.Contains("Spectator"))
                return;

            Plugin.RecordUnpassedPlayerScore(__instance, forceFailed: true);
        }
    }
}
