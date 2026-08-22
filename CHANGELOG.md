# Changelog

All notable changes to Lazer Lens are documented here.

## [1.3.2] - 2026-08-22

### Added
- **Dedicated Archive Tab**: Full past sessions viewer with search, filtering, and detailed statistics.
- **Repositioned Header Search Bar**: Search input is now cleanly integrated directly into the play history header row.

### Fixed
- **Failed Plays Tracking**: Fixed Harmony postfix signature for `Player.ConcludeFailedScore` and improved reflection to reliably detect failed plays via `HealthProcessor.HasFailed`, health value, and fail overlay.
- **Active Tab Bar Indicator Alignment**: Aligned the active tab pill indicator with the horizontal baseline across all tabs.
- **Scrollbar Overlap on Session Cards**: Added right padding to the saved sessions list so the scrollbar no longer overlaps session cards or PP badges.
- **Saved Sessions Scroll Clipping**: Fixed scroll clipping in the archive list by separating the header into an isolated container.
- **Settings Checkbox Alignment**: Aligned all settings options without staircase indentation.
- **Uniform Metric Card Spacing**: Standardized gaps between metric cards and sections.

## [1.3.0] - 2026-08-21

### Added
- **OsuCcWaveOverlay Migration**: Upgraded overlay to `OsuCcWaveOverlay` with animated wave transitions and tabbed navigation (`Session` and `Settings`).
- **Integrated Settings Tab**: Settings are now natively embedded as a dedicated tab, replacing the floating modal.
- **Scrollable Floating Session Selector**: Redesigned session dropdown with shadow effect, fixed width, and scrolling for past sessions.

### Fixed
- **InvalidOperationException Crash**: Fixed AutoSizeAxes conflict on the play history container that caused error notifications during gameplay and overlay open.

## [1.2.0] - 2026-08-21

### Added
- **Plugin Documents**: In-game README and Changelog tabs integrated directly into the osu!cc plugin manager.
- **Fail Play Capture**: Full tracking of failed scores (HP = 0) with rank `F`.
- **Modern Settings Button**: Redesigned header settings button matching osu!cc plugin manager tab styling.
- **Native 3.0.0 Architecture**: Migrated to `PluginPatch<LazerLensPlugin>` and native header icon integration.

### Fixed
- Fixed unpassed/quit plays discarding genuine failed scores when `TrackRetries` is disabled.
- Fixed overlay button positioning in SongSelect.

## [1.1.2] - 2026-08-18

### Added
- Compact mode and Unstable Rate (UR) visibility toggles.
- CSV export for session history.
