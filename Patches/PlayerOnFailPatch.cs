using osu.Game.Screens.Play;
using osucc.Core;
using osucc.Plugin;

namespace LazerLens.Patches
{
    /// <summary>
    /// Hooks Player.OnFail (called immediately when player health drops to 0) to capture failed scores.
    /// </summary>
    public sealed class PlayerOnFailPatch : PluginPatch<LazerLensPlugin>
    {
        public PlayerOnFailPatch(LazerLensPlugin plugin, IOsuCcPluginHost host)
            : base(plugin, host, typeof(Player), "OnFail", MethodType.Postfix)
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
