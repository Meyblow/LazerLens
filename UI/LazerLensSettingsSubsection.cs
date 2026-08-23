using osu.Framework.Localisation;
using osu.Game.Overlays.Settings;
using osucc.Plugin;
using LazerLens.Models;
using LazerLens.Services;

namespace LazerLens.UI
{
    public partial class LazerLensSettingsSubsection : SettingsSubsection
    {
        protected override LocalisableString Header => LazerLensStrings.Name;

        public LazerLensSettingsSubsection(PluginSettings settings, LazerLensService service, System.Action onExportRequested, System.Action onOpenDirectoryRequested)
        {
            // 1. Metrics & Display
            Add(new SettingsEnumDropdown<DefaultSortMode>
            {
                LabelText = LazerLensStrings.SettingsDefaultSortCaption,
                Current = service.DefaultSort,
            });

            Add(new SettingsEnumDropdown<PpDisplayMode>
            {
                LabelText = LazerLensStrings.SettingsPpDisplayCaption,
                Current = service.PpDisplay,
            });

            Add(new SettingsEnumDropdown<AccuracyCalculationMode>
            {
                LabelText = LazerLensStrings.SettingsAccCalcCaption,
                Current = service.AccuracyCalculation,
            });

            this.AddCheckbox(
                settings,
                "highlight_ur",
                true,
                LazerLensStrings.SettingsHighlightURCaption,
                LazerLensStrings.SettingsHighlightURSubtitle
            );

            this.AddCheckbox(
                settings,
                "show_mods_history",
                true,
                LazerLensStrings.SettingsShowModsCaption,
                LazerLensStrings.SettingsShowModsSubtitle
            );

            this.AddCheckbox(
                settings,
                "show_difficulty_rating",
                true,
                LazerLensStrings.SettingsShowDiffCaption,
                LazerLensStrings.SettingsShowDiffSubtitle
            );

            this.AddCheckbox(
                settings,
                "compact_mode",
                true,
                LazerLensStrings.SettingsCompactHistoryCaption,
                LazerLensStrings.SettingsCompactHistorySubtitle
            );

            // 2. Session Management
            Add(new SettingsEnumDropdown<SessionSplitThreshold>
            {
                LabelText = LazerLensStrings.SettingsSessionSplitCaption,
                Current = service.SessionSplit,
            });

            Add(new SettingsEnumDropdown<AfkPauseTimeout>
            {
                LabelText = LazerLensStrings.SettingsAfkPauseCaption,
                Current = service.AfkPause,
            });

            this.AddCheckbox(
                settings,
                "enable_session_pause",
                false,
                LazerLensStrings.SettingsEnablePauseCaption,
                LazerLensStrings.SettingsEnablePauseSubtitle
            );

            this.AddCheckbox(
                settings,
                "auto_export_csv",
                false,
                LazerLensStrings.SettingsAutoExportCsvCaption,
                LazerLensStrings.SettingsAutoExportCsvSubtitle
            );

            Add(new SettingsEnumDropdown<ArchiveRetentionLimit>
            {
                LabelText = LazerLensStrings.SettingsRetentionLimitCaption,
                Current = service.ArchiveRetention,
            });

            // 3. Recording Filters
            this.AddCheckbox(
                settings,
                "track_standard",
                true,
                "Track osu! (Standard) plays",
                "Records standard mode plays in current session"
            );

            this.AddCheckbox(
                settings,
                "track_taiko",
                true,
                "Track osu!taiko plays",
                "Records taiko mode plays in current session"
            );

            this.AddCheckbox(
                settings,
                "track_catch",
                true,
                "Track osu!catch plays",
                "Records catch mode plays in current session"
            );

            this.AddCheckbox(
                settings,
                "track_mania",
                true,
                "Track osu!mania plays",
                "Records mania mode plays in current session"
            );

            this.AddCheckbox(
                settings,
                "track_custom_rulesets",
                true,
                LazerLensStrings.SettingsTrackCustomCaption,
                LazerLensStrings.SettingsTrackCustomSubtitle
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
                "ignore_nofail",
                false,
                LazerLensStrings.SettingsIgnoreNoFailCaption,
                LazerLensStrings.SettingsIgnoreNoFailSubtitle
            );

            this.AddCheckbox(
                settings,
                "ranked_loved_only",
                false,
                LazerLensStrings.SettingsRankedLovedOnlyCaption,
                LazerLensStrings.SettingsRankedLovedOnlySubtitle
            );

            // 4. Notifications
            Add(new SettingsEnumDropdown<PlayNotificationFilter>
            {
                LabelText = LazerLensStrings.SettingsNotifFilterCaption,
                Current = service.PlayNotifFilter,
            });

            this.AddCheckbox(
                settings,
                "notify_session_best",
                true,
                LazerLensStrings.SettingsNotifySessionBestCaption,
                LazerLensStrings.SettingsNotifySessionBestSubtitle
            );

            Add(new SettingsEnumDropdown<MilestoneNotificationMode>
            {
                LabelText = LazerLensStrings.SettingsMilestonesCaption,
                Current = service.Milestones,
            });

            // 5. Overlay & Toolbar
            this.AddCheckbox(
                settings,
                "auto_open_overlay_on_pass",
                false,
                LazerLensStrings.SettingsAutoOpenOverlayCaption,
                LazerLensStrings.SettingsAutoOpenOverlaySubtitle
            );

            Add(new SettingsEnumDropdown<ToolbarBadgeMode>
            {
                LabelText = LazerLensStrings.SettingsToolbarBadgeCaption,
                Current = service.ToolbarBadge,
            });

            Add(new SettingsEnumDropdown<SearchBarPosition>
            {
                LabelText = LazerLensStrings.SettingsSearchBarPositionCaption,
                Current = service.SearchPosition,
            });

            Add(new SettingsTextBox
            {
                LabelText = LazerLensStrings.SettingsToolbarBadgeColorCaption,
                Current = service.ToolbarBadgeColor,
            });

            Add(new SettingsEnumDropdown<ShareFormattingMode>
            {
                LabelText = LazerLensStrings.SettingsShareFormattingCaption,
                Current = service.ShareFormatting,
            });

            Add(new SettingsDoubleActionRow(
                LazerLensStrings.SettingsOpenDirectory, onOpenDirectoryRequested,
                LazerLensStrings.SettingsExportCsv, onExportRequested
            ));
        }
    }
}
