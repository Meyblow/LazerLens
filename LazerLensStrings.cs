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
        public static LocalisableString TabSession => OsuCcLocalisation.Get(getKey(nameof(TabSession)), "Session");
        public static LocalisableString TabArchive => OsuCcLocalisation.Get(getKey(nameof(TabArchive)), "Past Sessions");
        public static LocalisableString TabSettings => OsuCcLocalisation.Get(getKey(nameof(TabSettings)), "Settings");

        public static LocalisableString ArchiveSavedSessions(int count) => OsuCcLocalisation.Get(getKey(nameof(ArchiveSavedSessions)), "SAVED SESSIONS ({0})", count);
        public static LocalisableString ArchiveEmptyTitle => OsuCcLocalisation.Get(getKey(nameof(ArchiveEmptyTitle)), "No archived sessions");
        public static LocalisableString ArchiveEmptySubtitle => OsuCcLocalisation.Get(getKey(nameof(ArchiveEmptySubtitle)), "Your previous play sessions will be saved and listed here.");
        public static LocalisableString ArchiveSelectPrompt => OsuCcLocalisation.Get(getKey(nameof(ArchiveSelectPrompt)), "Select a session from the list on the left to view its details.");
        public static LocalisableString ArchivePlaysCount(int count) => count == 1
            ? OsuCcLocalisation.Get(getKey("ArchivePlaysCountSingle"), "1 play")
            : OsuCcLocalisation.Get(getKey(nameof(ArchivePlaysCount)), "{0} plays", count);

        public static LocalisableString TooltipViewBeatmap => OsuCcLocalisation.Get(getKey(nameof(TooltipViewBeatmap)), "Click to view beatmap info in overlay");
        public static LocalisableString TooltipLocalBeatmap => OsuCcLocalisation.Get(getKey(nameof(TooltipLocalBeatmap)), "Local beatmap (no online ID)");
        public static LocalisableString HeaderSettingsTooltip => OsuCcLocalisation.Get(getKey(nameof(HeaderSettingsTooltip)), "Lazer Lens Settings");
        public static LocalisableString SettingsSectionGameplay => OsuCcLocalisation.Get(getKey(nameof(SettingsSectionGameplay)), "GAMEPLAY & TRACKING");
        public static LocalisableString SettingsSectionVisuals => OsuCcLocalisation.Get(getKey(nameof(SettingsSectionVisuals)), "INTERFACE");
        public static LocalisableString SettingsSectionData => OsuCcLocalisation.Get(getKey(nameof(SettingsSectionData)), "DATA & STORAGE");

        public static LocalisableString SettingsNotificationsCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsNotificationsCaption)), "Notifications after play");
        public static LocalisableString SettingsNotificationsSubtitle => OsuCcLocalisation.Get(getKey(nameof(SettingsNotificationsSubtitle)), "Shows a summary notification in the corner after every completed beatmap");

        public static LocalisableString SettingsTrackRetriesCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsTrackRetriesCaption)), "Track retried plays");
        public static LocalisableString SettingsTrackRetriesSubtitle => OsuCcLocalisation.Get(getKey(nameof(SettingsTrackRetriesSubtitle)), "Records retries in the session history list alongside passed scores");

        public static LocalisableString SettingsCompactHistoryCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsCompactHistoryCaption)), "Compact history UI");
        public static LocalisableString SettingsCompactHistorySubtitle => OsuCcLocalisation.Get(getKey(nameof(SettingsCompactHistorySubtitle)), "Makes the play history list more compact to fit more scores on screen");

        public static LocalisableString SettingsShowURCaption => OsuCcLocalisation.Get(getKey(nameof(SettingsShowURCaption)), "Show Unstable Rate");
        public static LocalisableString SettingsShowURSubtitle => OsuCcLocalisation.Get(getKey(nameof(SettingsShowURSubtitle)), "Displays the UR for each play in the history list and session metrics");

        public static LocalisableString SettingsOpenDirectory => OsuCcLocalisation.Get(getKey(nameof(SettingsOpenDirectory)), "Open sessions directory");
        public static LocalisableString SettingsExportCsv => OsuCcLocalisation.Get(getKey(nameof(SettingsExportCsv)), "Export sessions to CSV");

        public static LocalisableString OverlayNoPlays => OsuCcLocalisation.Get(getKey(nameof(OverlayNoPlays)), "No plays yet in this session.");
        public static LocalisableString OverlaySessionTime => OsuCcLocalisation.Get(getKey(nameof(OverlaySessionTime)), "SESSION TIME");
        public static LocalisableString OverlayTotalPlays => OsuCcLocalisation.Get(getKey(nameof(OverlayTotalPlays)), "TOTAL PLAYS");
        public static LocalisableString OverlayAvgAccuracy => OsuCcLocalisation.Get(getKey(nameof(OverlayAvgAccuracy)), "AVG ACCURACY");
        public static LocalisableString OverlayMaxCombo => OsuCcLocalisation.Get(getKey(nameof(OverlayMaxCombo)), "MAX COMBO");
        public static LocalisableString OverlaySessionPPGain(string gain) => OsuCcLocalisation.Get(getKey(nameof(OverlaySessionPPGain)), "Session PP: {0}", gain);

        public static LocalisableString AddedToTracker(string title, string acc, string grade, string pp) => OsuCcLocalisation.Get(getKey(nameof(AddedToTracker)), "Added to tracker: {0}\nAcc: {1}% \u2022 Grade: {2}{3}", title, acc, grade, pp);

        public static LocalisableString ExportSuccess(string path) => OsuCcLocalisation.Get(getKey(nameof(ExportSuccess)), "Sessions exported to {0}", path);
        public static LocalisableString ExportFailed(string error) => OsuCcLocalisation.Get(getKey(nameof(ExportFailed)), "Failed to export sessions: {0}", error);

        public static LocalisableString DropdownLiveSession => OsuCcLocalisation.Get(getKey(nameof(DropdownLiveSession)), "● Live Session");
        public static LocalisableString DropdownArchivedSession => OsuCcLocalisation.Get(getKey(nameof(DropdownArchivedSession)), "Archived Session");
        public static LocalisableString DropdownCurrentActive => OsuCcLocalisation.Get(getKey(nameof(DropdownCurrentActive)), "Current active session");
        public static LocalisableString TimeStartedJustNow => OsuCcLocalisation.Get(getKey(nameof(TimeStartedJustNow)), "Started just now");
        public static LocalisableString TimeStartedAt(string time) => OsuCcLocalisation.Get(getKey(nameof(TimeStartedAt)), "Started at {0} (running)", time);
        public static LocalisableString TimeArchived(string time) => OsuCcLocalisation.Get(getKey(nameof(TimeArchived)), "Archived: {0}", time);
        public static LocalisableString PlaysPassFail(int passes, int fails) => OsuCcLocalisation.Get(getKey(nameof(PlaysPassFail)), "{0} pass \u2022 {1} fail", passes, fails);
        public static LocalisableString PlaysRecorded(int plays) => plays == 1
            ? OsuCcLocalisation.Get(getKey("PlaysRecordedSingle"), "1 recorded play")
            : OsuCcLocalisation.Get(getKey(nameof(PlaysRecorded)), "{0} recorded plays", plays);
        public static LocalisableString AccPlaysUr(int count, string ur) => count == 1
            ? OsuCcLocalisation.Get(getKey("AccPlaysUrSingle"), "1 recorded play{0}", ur)
            : OsuCcLocalisation.Get(getKey(nameof(AccPlaysUr)), "{0} recorded plays{1}", count, ur);
        public static LocalisableString AvgUr(string ur) => OsuCcLocalisation.Get(getKey(nameof(AvgUr)), " \u2022 {0} avg UR", ur);
        public static LocalisableString BestScoreTitle => OsuCcLocalisation.Get(getKey(nameof(BestScoreTitle)), "SESSION BEST SCORE");
        public static LocalisableString BestScoreEmpty => OsuCcLocalisation.Get(getKey(nameof(BestScoreEmpty)), "No scores recorded yet in this session");
        public static LocalisableString BestScoreDetail(string grade, string acc, string score, int combo, string pp, string status) =>
            OsuCcLocalisation.Get(getKey(nameof(BestScoreDetail)), "Grade: {0} \u2022 {1}% \u2022 {2} pts \u2022 {3}x combo{4} \u2022 {5}", grade, acc, score, combo, pp, status);
        public static LocalisableString HistoryTitle(int count) => count == 1
            ? OsuCcLocalisation.Get(getKey("HistoryTitleSingle"), "PLAY HISTORY (1 PLAY)")
            : OsuCcLocalisation.Get(getKey(nameof(HistoryTitle)), "PLAY HISTORY ({0} PLAYS)", count);
        public static LocalisableString HistoryEmpty => OsuCcLocalisation.Get(getKey(nameof(HistoryEmpty)), "No beatmaps played in this session yet. Go set some scores!");
        public static LocalisableString FilterEmpty => OsuCcLocalisation.Get(getKey(nameof(FilterEmpty)), "No beatmaps found matching current filters.");
        public static LocalisableString SearchPlaceholder => OsuCcLocalisation.Get(getKey(nameof(SearchPlaceholder)), "Search maps...");
        public static LocalisableString FilterAll => OsuCcLocalisation.Get(getKey(nameof(FilterAll)), "All");
        public static LocalisableString FilterPass => OsuCcLocalisation.Get(getKey(nameof(FilterPass)), "Passed");
        public static LocalisableString FilterFail => OsuCcLocalisation.Get(getKey(nameof(FilterFail)), "Failed");

        public static LocalisableString FilterCategoryRuleset => OsuCcLocalisation.Get(getKey(nameof(FilterCategoryRuleset)), "Ruleset:");
        public static LocalisableString FilterCategoryOutcome => OsuCcLocalisation.Get(getKey(nameof(FilterCategoryOutcome)), "Outcome:");
        public static LocalisableString FilterCategoryStatus => OsuCcLocalisation.Get(getKey(nameof(FilterCategoryStatus)), "Status:");
        public static LocalisableString FilterCategorySort => OsuCcLocalisation.Get(getKey(nameof(FilterCategorySort)), "Sort by:");

        public static LocalisableString FilterStatusAll => OsuCcLocalisation.Get(getKey(nameof(FilterStatusAll)), "All");
        public static LocalisableString FilterStatusRanked => OsuCcLocalisation.Get(getKey(nameof(FilterStatusRanked)), "Ranked");
        public static LocalisableString FilterStatusLoved => OsuCcLocalisation.Get(getKey(nameof(FilterStatusLoved)), "Loved");
        public static LocalisableString FilterStatusGraveyard => OsuCcLocalisation.Get(getKey(nameof(FilterStatusGraveyard)), "Graveyard");

        public static LocalisableString FilterOrderDesc => OsuCcLocalisation.Get(getKey(nameof(FilterOrderDesc)), "Descending");
        public static LocalisableString FilterOrderAsc => OsuCcLocalisation.Get(getKey(nameof(FilterOrderAsc)), "Ascending");

        public static LocalisableString SortRecent => OsuCcLocalisation.Get(getKey(nameof(SortRecent)), "Recent");
        public static LocalisableString SortScore => OsuCcLocalisation.Get(getKey(nameof(SortScore)), "Score");
        public static LocalisableString SortAccuracy => OsuCcLocalisation.Get(getKey(nameof(SortAccuracy)), "Accuracy");
        public static LocalisableString SortAccShort => OsuCcLocalisation.Get(getKey(nameof(SortAccShort)), "Acc");
        public static LocalisableString SortHits => OsuCcLocalisation.Get(getKey(nameof(SortHits)), "HITS");
        public static LocalisableString SortPP => OsuCcLocalisation.Get(getKey(nameof(SortPP)), "PP");
        public static LocalisableString SortCombo => OsuCcLocalisation.Get(getKey(nameof(SortCombo)), "Combo");
        public static LocalisableString SortGrade => OsuCcLocalisation.Get(getKey(nameof(SortGrade)), "Grade");
        public static LocalisableString SortDifficulty => OsuCcLocalisation.Get(getKey(nameof(SortDifficulty)), "Difficulty");

        public static LocalisableString ArchiveBanner(string time) => OsuCcLocalisation.Get(getKey(nameof(ArchiveBanner)), "Viewing archived session from {0}", time);
        public static LocalisableString ReturnToLive => OsuCcLocalisation.Get(getKey(nameof(ReturnToLive)), "[ Return to Live ]");

        public static LocalisableString SessionSummaryDetail(int playCount, double topPP, double avgAcc) =>
            OsuCcLocalisation.Get(getKey(nameof(SessionSummaryDetail)), "{0} plays \u2022 {1:F0}pp \u2022 {2:F1}% avg", playCount, topPP, avgAcc);

        public static LocalisableString DeleteSession => OsuCcLocalisation.Get(getKey(nameof(DeleteSession)), "Delete session");

        public static LocalisableString ContextMenuOpenInFolder => OsuCcLocalisation.Get(getKey(nameof(ContextMenuOpenInFolder)), "Open in folder");
        public static LocalisableString ContextMenuPinSession => OsuCcLocalisation.Get(getKey(nameof(ContextMenuPinSession)), "Pin session");
        public static LocalisableString ContextMenuUnpinSession => OsuCcLocalisation.Get(getKey(nameof(ContextMenuUnpinSession)), "Unpin session");
        public static LocalisableString ContextMenuSetNote => OsuCcLocalisation.Get(getKey(nameof(ContextMenuSetNote)), "Set session note...");
        public static LocalisableString ContextMenuEditNote => OsuCcLocalisation.Get(getKey(nameof(ContextMenuEditNote)), "Edit session note...");
        public static LocalisableString ContextMenuDeleteSession => OsuCcLocalisation.Get(getKey(nameof(ContextMenuDeleteSession)), "Delete session");

        public static LocalisableString DialogDeleteConfirmTitle => OsuCcLocalisation.Get(getKey(nameof(DialogDeleteConfirmTitle)), "Delete Saved Session");
        public static LocalisableString DialogDeleteConfirmBody => OsuCcLocalisation.Get(getKey(nameof(DialogDeleteConfirmBody)), "Are you sure you want to permanently delete this session and its play history?");
        public static LocalisableString DialogSetNoteTitle => OsuCcLocalisation.Get(getKey(nameof(DialogSetNoteTitle)), "Session Note");
        public static LocalisableString DialogSetNoteDescription => OsuCcLocalisation.Get(getKey(nameof(DialogSetNoteDescription)), "Enter a custom note or description for this session:");
        public static LocalisableString DialogSetNotePlaceholder => OsuCcLocalisation.Get(getKey(nameof(DialogSetNotePlaceholder)), "e.g. My first top play");
        public static LocalisableString DialogSave => OsuCcLocalisation.Get(getKey(nameof(DialogSave)), "Save");
        public static LocalisableString DialogCancel => OsuCcLocalisation.Get(getKey(nameof(DialogCancel)), "Cancel");
    }
}