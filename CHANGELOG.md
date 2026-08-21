# Changelog

All notable changes to Lazer Lens are documented here.

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
