using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace LazerLens.UI
{
    public partial class LazerLensSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => "Lazer Lens";

        public LazerLensSettingsSubsection(PluginSettings settings, System.Action onExportRequested, System.Action onOpenDirectoryRequested)
        {
            this.AddCheckbox(
                settings,
                "notify_on_play",
                true,
                "Notifications after play",
                "Shows a summary notification in the corner after every completed beatmap"
            );

            this.AddCheckbox(
                settings,
                "celebrate_best",
                true,
                "Celebrate session bests",
                "Plays celebratory particle effects when you set a new best score in the current session"
            );

            this.AddCheckbox(
                settings,
                "track_retries",
                true,
                "Track retried plays",
                "Records retries in the session history list alongside passed scores"
            );

            this.AddCheckbox(
                settings,
                "compact_mode",
                false,
                "Compact history UI",
                "Makes the play history list more compact to fit more scores on screen"
            );

            this.AddCheckbox(
                settings,
                "show_ur",
                true,
                "Show Unstable Rate",
                "Displays the UR for each play in the history list"
            );

            Add(new SettingsDoubleActionRow(
                "Open sessions directory", onOpenDirectoryRequested,
                "Export sessions to CSV", onExportRequested
            ));
        }
    }
}


