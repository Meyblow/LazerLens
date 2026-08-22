using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;

namespace LazerLens.UI
{
    public partial class LazerLensSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => LazerLensStrings.Name;

        public LazerLensSettingsSubsection(PluginSettings settings, System.Action onExportRequested, System.Action onOpenDirectoryRequested)
        {
            this.AddCheckbox(
                settings,
                "notify_on_play",
                true,
                LazerLensStrings.SettingsNotificationsCaption,
                LazerLensStrings.SettingsNotificationsSubtitle
            );

            this.AddCheckbox(
                settings,
                "track_retries",
                false,
                LazerLensStrings.SettingsTrackRetriesCaption,
                LazerLensStrings.SettingsTrackRetriesSubtitle
            );

            this.AddCheckbox(
                settings,
                "compact_mode",
                true,
                LazerLensStrings.SettingsCompactHistoryCaption,
                LazerLensStrings.SettingsCompactHistorySubtitle
            );

            this.AddCheckbox(
                settings,
                "show_ur",
                true,
                LazerLensStrings.SettingsShowURCaption,
                LazerLensStrings.SettingsShowURSubtitle
            );

            Add(new SettingsDoubleActionRow(
                LazerLensStrings.SettingsOpenDirectory, onOpenDirectoryRequested,
                LazerLensStrings.SettingsExportCsv, onExportRequested
            ));
        }
    }
}


