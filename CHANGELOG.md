# Changelog

All notable changes to Lazer Lens are documented here.

## [1.4.4] - 2026-08-22

### Added
- **Active Toolbar Button Highlight**: Inherited `ToolbarOverlayToggleButton` for `LazerLensToolbarButton` and bound overlay visibility state so the toolbar button automatically lights up with the active accent background when the overlay is open, matching standard osu! toolbar overlays.

## [1.4.3] - 2026-08-22

### Fixed
- **Overlay Invocation & Layout Exception**: Fixed layout issue where `dialogContainer` with `RelativeSizeAxes = Axes.Both` was nested within an `AutoSizeAxes.Y` parent container causing overlay loading failure, and removed redundant `CreateBackdrop` override.

## [1.4.2] - 2026-08-22

### Fixed
- **Metric Card Width & Spacing**: Replaced padding-based grid columns with a 7-column `GridContainer` (4 distributed columns + 3 exact 12px gaps) on both Live and Past Sessions tabs. Cards now stretch seamlessly across full width with uniform 12px spacing and generous space for all text metrics.

## [1.4.1] - 2026-08-22

### Added
- **Session Sorting Dropdown**: Quick sort saved sessions by Date, Top PP, or Play Count in the top-right of the sessions panel (pinned sessions remain pinned at top).
- **Dual Independent Scrolling**: Archive tab split into two independent parallel scrollable panes — scroll through saved sessions and song history side-by-side without page jumping.
- **Smooth Session Animations**: Animated collapse (`ResizeHeightTo(0)` + `FadeOut`) upon session deletion, scale pulse upon pinning/unpinning, and instant in-place note updates without scroll resets.

### Fixed
- **Pixel-Perfect Header & Tabs**: Aligned overlay header, tab control (40px height, flush underline), font sizes, and casing to strictly follow `AGENTS.md` and standard osu! dashboard overlay.
- **Top Baseline Alignment**: Aligned the top edge of the `SAVED SESSIONS` panel horizontally with the KPI metric cards.
- **Visual Breathing Room & Depth**: Added subtle 1px border outlines to metric cards and a golden trophy border to the Best Score banner to prevent visual clumping.
- **Clean Typography**: Refined all dropdowns, sort labels, and filter controls to use clean typography without emojis.

## [1.4.0] - 2026-08-22

### Added
- **Session Context Menu (Right Click)**: Added context menu on past session cards with options to:
  - 📂 **Open in Folder**: Open the exact session `.json` file in system file manager.
  - 📌 **Pin / Unpin Session**: Pin favorite sessions to the top of the archive list with a gold thumbtack icon.
  - 🏷️ **Set / Edit Session Note**: Modal dialog to add custom notes and names to sessions (e.g. *"My first top play"*).
  - 🗑️ **Delete Session**: Confirmation dialog to permanently delete saved session archives.
- **Default Settings Adjusted**: Defaults updated to `Notifications after play: ON`, `Track retried plays: OFF`, `Compact history UI: ON`, `Show Unstable Rate: ON`.

### Fixed
- **Mathematical Uniform Spacing**: Grid column paddings and vertical gaps across live and archive tabs calibrated to exact 8px uniform spacing.

## [1.3.7] - 2026-08-22

### Changed
- **Migrated to osucc.Data.IOsuCcStorage (VFS)**: Session storage now uses the native `Host.Data` Virtual File System (`osu-cc/data/lazer-lens/sessions/`). Automatic migration seamlessly moves any existing sessions from legacy locations into the new VFS structure.

## [1.3.6] - 2026-08-22

### Changed
- **Session Storage Location**: Saved sessions are now stored directly in `osu-cc/sessions/` (on Linux `~/.local/share/osu/osu-cc/sessions/`, on Windows `%APPDATA%\osu\osu-cc\sessions\`) instead of the plugin subfolder. Existing sessions are automatically migrated seamlessly.

### Fixed
- **Uniform Spacing & Layout Alignment**: Polished all horizontal card and vertical section spacings to ensure uniform alignment across the overlay.

## [1.3.5] - 2026-08-22

### Fixed
- **Plugin Metadata Binary Compatibility**: Rebuilt with updated `osucc.Api` packages guaranteeing binary compatibility across all `osu!cc` runtime versions without `MissingMethodException`.

## [1.3.4] - 2026-08-22

### Fixed
- **Plugin Metadata Compatibility**: Fixed `MissingMethodException` when loading `LazerLens.dll` on standard/legacy `osu!cc` hook installations by restoring backward-compatible 3-argument constructor binding for `OsuCcPluginAttribute`.

## [1.3.3] - 2026-08-22

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
