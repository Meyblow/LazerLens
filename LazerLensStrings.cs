using osu.Framework.Localisation;
using osucc.Localisation;

namespace LazerLens
{
    public static class LazerLensStrings
    {
        private const string prefix = "lazer-lens";
        private static string getKey(string name) => $"{prefix}:{name}";

        public static LocalisableString Name => OsuCcLocalisation.Get($"{prefix}:name", "Lazer Lens");
        public static LocalisableString Description => OsuCcLocalisation.Get($"{prefix}:description", "Tracks your play session metrics, play history, average accuracy, and session bests with an in-game overlay.");

        public static LocalisableString TooltipMain => OsuCcLocalisation.Get(getKey(nameof(TooltipMain)), "Lazer Lens");
        public static LocalisableString TooltipSub => OsuCcLocalisation.Get(getKey(nameof(TooltipSub)), "Session stats and history");

        public static LocalisableString SettingsNotificationsCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsNotificationsCaption)), "Notifications after play");
        public static LocalisableString SettingsCelebrateCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsCelebrateCaption)), "Celebrate session bests");
        public static LocalisableString SettingsTrackRetriesCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsTrackRetriesCaption)), "Track retried plays");
        public static LocalisableString SettingsCompactHistoryCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsCompactHistoryCaption)), "Compact history UI");
        public static LocalisableString SettingsShowURCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsShowURCaption)), "Show Unstable Rate");

        public static LocalisableString SettingsOpenDirectory => OsuCcLocalisation.Get(getKey(nameof(SettingsOpenDirectory)), "Open sessions directory");
        public static LocalisableString SettingsExportCsv => OsuCcLocalisation.Get(getKey(nameof(SettingsExportCsv)), "Export sessions to CSV");

        public static LocalisableString OverlayNoPlays => OsuCcLocalisation.Get(getKey(nameof(OverlayNoPlays)), "No plays yet in this session.");
        public static LocalisableString OverlaySessionTime => OsuCcLocalisation.Get(getKey(nameof(OverlaySessionTime)), "Session Time");
        public static LocalisableString OverlayTotalPlays => OsuCcLocalisation.Get(getKey(nameof(OverlayTotalPlays)), "Total Plays");
        public static LocalisableString OverlayAvgAccuracy => OsuCcLocalisation.Get(getKey(nameof(OverlayAvgAccuracy)), "Avg Accuracy");
        public static LocalisableString OverlayMaxCombo => OsuCcLocalisation.Get(getKey(nameof(OverlayMaxCombo)), "Max Combo");
        public static LocalisableString OverlaySessionPPGain(string gain) => OsuCcLocalisation.Get(getKey(nameof(OverlaySessionPPGain)), "Session PP: {0}", gain);

        public static LocalisableString NotificationNewBest => OsuCcLocalisation.Get(getKey(nameof(NotificationNewBest)), "New Session Best!");
        public static LocalisableString NotificationNewBestDetails(string artist, string title, string acc) => OsuCcLocalisation.Get(getKey(nameof(NotificationNewBestDetails)), "New Session Best!\n{0} - {1} ({2}%)", artist, title, acc);
        
        public static LocalisableString ExportSuccess(string path) => OsuCcLocalisation.Get(getKey(nameof(ExportSuccess)), "Sessions exported to {0}", path);
        public static LocalisableString ExportFailed(string error) => OsuCcLocalisation.Get(getKey(nameof(ExportFailed)), "Failed to export sessions: {0}", error);
    }
}

