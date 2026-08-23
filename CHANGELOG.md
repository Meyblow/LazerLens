# Changelog

All notable changes to Lazer Lens are documented here.

## [2.2.5] - 2026-08-23

### Fixed
- **Session Goal Spacing**: Dynamically bypass layout autosizing on `liveGraphContainer` when empty, eliminating the unwanted double vertical gap below `SessionGoalWidget` and ensuring uniform 8px spacing directly to `BestScoreBanner`.
- **Analytics Timeline Chart Layout**: Rebuilt `PpGrowthTimelineChart` using proper `MarginPadding` and a 12% vertical margin buffer, preventing chart vertices from clipping against the bottom edge and aligning date labels cleanly below the canvas.

## [2.2.4] - 2026-08-23

### UI & Styling Refinements
- **Unified Borderless Design**: Removed borders and outlines across all UI blocks, metric cards, session goal progress bar, best score banner, play history items, and analytics charts for a modern, flat aesthetic.
- **Consistent 8px Spacing**: Standardized vertical and horizontal gaps between all blocks to exactly 8px across the Live Session and Archive Session Detail panels.
- **Header Button Visual Parity**: Unified `WarmupToggleButton` and `ShareSessionButton` to share identical geometry, padding, background colours, `SemiBold` typography, and smooth hover animations.
- **Archive Filter Dropdown Fix**: Repositioned the archive session sort dropdown and header to prevent clipping and menu overlap at the top of the archive list container.

## [2.2.3] - 2026-08-23

### Architecture, Stability & Performance
- **Registered PlayerConcludeFailedScorePatch**: Added `PlayerConcludeFailedScorePatch` to patch array for complete coverage across base `Player` and `SubmittingPlayer` gameplay paths with `TryMarkPlayerRecorded` deduplication.
- **Thread-Safe PP Calculation**: Eliminated background worker thread state mutation in `calculateAndAssignPP`, enforcing dispatch via `Host.Scheduler`.
- **Hot-Reload Cleanup**: Added explicit clearing of `recordedPlayerHashes` under lock prior to instance reset in `Dispose()`.
- **Complete Settings Unbinding**: Implemented `UnbindAllSettings()` covering all 25+ settings Bindables on plugin disposal.
- **Async Throttled Storage Pruning**: Moved `PruneOldSessions` to asynchronous startup execution with 24-hour throttling, keeping `Dispose()` lightweight and instant.
- **Thread-Safe & Non-Blocking Storage**: Protected all file reads/writes with `lock (storageLock)` and made archive UI file operations fire-and-forget in background threads to avoid UI micro-stutters.

## [2.2.2] - 2026-08-23

### Added & Improved
- **Scrollable Leaderboard Cards in Analytics**: Integrated independent `OsuScrollContainer`s with sticky headers into **MOST PLAYED BEATMAPS** and **TOP BEATMAP CREATORS**, allowing users to scroll through expanded leaderboards (up to 15 entries each).

### Fixed
- **Notification Emoji Rendering (??)**: Removed unsupported Unicode emoji characters from toast notifications and chart tooltips that appeared as `??` in osu!'s Torus font renderer.

## [2.2.1] - 2026-08-23

### Fixed & Polished
- **Smooth Spline PP Growth Timeline Chart**: Replaced polygonal straight line segments with smooth Catmull-Rom spline interpolation, creating modern, organically curved transitions between data points without sharp corners.
- **Analytics Tab Scrolling**: Fixed vertical mouse wheel scrolling in the Analytics tab. Corrected `TopFavoritesLeaderboard` height calculation and added bottom padding so all leaderboards and cards are fully viewable.
- **Archive Sort Dropdown Alignment**: Aligned the `Date` sort dropdown button vertically to the exact centerline of `SAVED SESSIONS (X)` on the left.

## [2.2.0] - 2026-08-23

### Added
- **Session Goal Achieved Notifications**: In-game toast notifications (`🎯 Session goal achieved: ...! 🎉`) and golden glowing widget highlight whenever your active session goal is completed (Play Count, Session PP gain, or Accuracy).

### Fixed & Polished
- **PP Growth Timeline Chart**: Fixed date label overlap at 0 PP in bottom-left corner. Added dedicated footer row for start/end date labels and accurately aligned min, mid, and max PP values with horizontal gridlines.
- **Archive Detail Scrollbar Overlap**: Added dedicated 24px right padding so the vertical scrollbar track never overlaps history cards, metric blocks, or the Share Session button.
- **Archive Sort Dropdown Direction**: Restructured the saved sessions list header so the sort dropdown menu strictly opens downwards instead of rolling up above the header.

## [2.1.1] - 2026-08-23

### Redesigned & Polished
- **Full-Width 52-Week Activity Heatmap (График активности)**: Completely rebuilt using responsive `GridContainer` distribution. The 52-week grid now expands to 100% width of the card without empty side gaps, with month names along the top and day-of-week indicators (`Mon`, `Wed`, `Fri`) on the left.
- **Rich Chronological Session PP & Accuracy Timeline (График динамики)**: Reworked data pipeline to plot each saved session and live plays chronologically. Added horizontal grid guidelines, date markers on X-axis, total gain pill summary, and interactive glowing dots with detailed session tooltips.

## [2.1.0] - 2026-08-23

### Added
- **Global Profile Analytics Tab («📊 Аналитика»)**: Comprehensive profile-wide analytics with annual 52-week activity heatmap, cumulative PP & accuracy timeline, mod usage breakdown, star rating distribution, most played beatmaps, and top beatmap creators leaderboards.
- **Social Share Formatting Options**: Added settings toggle for session export format (`Markdown`, `Plain Text`, `HTML`) and automatic ruleset indicator badges (🔴 osu!, 🥁 taiko, 🍎 catch, 🎹 mania) for top scores.
- **Detailed Session Progression Tooltips**: Hovering over graph points now displays individual map PP gain alongside cumulative session PP gain.

### Fixed & Improved
- **Archive Sort Dropdown Direction**: Restructured archive left pane hierarchy so the sort dropdown menu (`Date`, `Top PP`, `Play Count`) opens downwards reliably.
- **Top Beatmap Creators (BeatmapMapper) Tracking**: Recorded mapper usernames in play records and persisted them in session JSON archives.
- **Responsive 52-Week Activity Heatmap**: Centered 52-week heatmap grid cleanly across card width without horizontal scrolling.
- **Compact Cumulative PP Chart**: Reduced chart height to 120px with padded boundaries to prevent line clipping.
- **Overlay Spacing Polish**: Refined vertical content spacing and metric card gaps for a sleek and compact layout.
- **Scrollbar Margins**: Increased archive list padding to prevent scrollbar overlap with session cards.

## [2.0.0] - 2026-08-23

### Added
- **Live Session Progress Graph & Trends**: Dynamic vector progress chart (`SessionProgressGraph`) visualizing PP progression, accuracy %, and star rating curve across the session with interactive metric switching and point hover tooltips.
- **Session Goals & Target Tracking**: Interactive goal system (`SessionGoalWidget` and `SetGoalDialog`) with animated progress bar for tracking Session PP (+PP), Play Count, or Accuracy targets with completion status.
- **Choke & "If FC" PP Analyzer**: Automatic choke detection for plays with 1–3 misses or combo breaks losing >8% PP, calculating simulated Full Combo PP (`CalculateIfFcPerformanceAsync`), with a 💔 icon badge and detailed hover tooltip (`Choke: Xpp if FC (-Ypp)`).
- **Warmup Mode**: Quick header warmup mode toggle (`☕ Warmup`) marking warmup plays with orange badges and optionally isolating them from main session statistics.
- **Session Share Card / Report Exporter**: Quick session report exporter (`📤 Share`) generating a formatted Discord/Markdown summary copied to clipboard and saved to user's exports folder.
- **Play Activity Calendar**: GitHub-style interactive heatmap grid (`SessionActivityCalendar`) in the Archive tab displaying play volume across days and weeks with hover tooltips.
- **Session Note Dialog Root Presentation**: Modal session note dialogs now attach directly to the root game viewport for full-screen backdrop and crisp pop-in animations.

## [1.5.0] - 2026-08-23

### Added
- **Custom Rulesets Support**: Full tracking and display support for Sentakki, Tau, Soyokaze, Swing, and all custom rulesets with dynamic hit results breakdown, ruleset icon display, dedicated "Custom" multi-select filter tab, performance points calculation, and a tracking filter toggle in settings.
- **Extended Search Bar & Alignment Setting**: Extended search bar width (420px) in both Live and Past Sessions views with a new `SearchBarPosition` setting (`Right` [default], `Centre`).
- **Toolbar Badge Fill Colour Customization**: Added customizable badge fill color setting (`ToolbarBadgeColor`) with instant live preview on the toolbar button.
- **5 Comprehensive Settings Subsections**: Extended settings for Metrics & Display, Session Management, Recording Filters, Notifications, and Overlay/Toolbar customization.

### Fixed
- **Performance Points Display**: Robust multi-tier PP calculation and main-thread asynchronous update ensuring played scores reliably display calculated PP without remaining `-`.
- **Accurate Hits Per Ruleset**: Tailored hit count columns and compact typography for osu! (4 hits), osu!taiko (3 hits), osu!catch (4 hits), osu!mania (6 hits: MAX/300/200/100/50/X in 96px column), and dynamic custom ruleset hit statistics.
- **Past Sessions Scrollbar Overlap**: Added generous padding and column separation so the scrollbar track does not overlap session cards.
- **Past Sessions Sort Dropdown Layering**: Corrected Z-index / depth layering so the archive sort dropdown menu always opens on top of session cards.
- **Session Note Dialog Dismissal**: Fixed mouse and focus event handling so the note dialog remains open until explicitly saved or cancelled.
- **Fail Play Capture**: Added hooks for `SubmittingPlayer.ConcludeFailedScore` and `Player.PerformFail` ensuring failed plays are reliably captured with `F` grade.
- **Open in Folder**: Implemented `OpenSessionFile` to open and highlight the specific session `.json` file in Windows Explorer (`explorer.exe /select,...`).
- **Live Reactive Settings**: Bound all history visual settings (`PP display mode`, `Highlight UR`, `Show mods in history`, `Show difficulty rating`, `Show UR`, `Compact mode`) to re-render history cards instantly upon setting changes without reopening the overlay.

## [1.4.7] - 2026-08-22

### Fixed
- **Tab Header Line Alignment**: Adjusted `LazerLensTabControl` height to 47px and tab item margins/bar baseline to align the active tab accent underline with the header separator line.
- **Initial Tab Selection & Screen Mutual Exclusion**: Added proper `BypassAutoSizeAxes` handling to hidden overlay tabs so initial opening renders the live session tab cleanly.
- **Beatmap Overlay Navigation**: Enhanced `openBeatmap` and history item click handlers to resolve `BeatmapSetOverlay` through `OsuGame` fallbacks.
- **Clean Empty Session UI**: Automatically hide the filter panel during live sessions with 0 plays for a clean interface.
- **Modal Dialog Interaction**: Added backdrop-click dismissal and focus management to `SessionNoteDialog`.

## [1.4.6] - 2026-08-22

### Fixed
- **Overlay Registration & Layout Exception**: Removed manual `Height` assignment on `archiveSortDropdown` (`OsuEnumDropdown` inherits `AutoSizeAxes.Y`), fixing `InvalidOperationException: The height of a CompositeDrawable with AutoSizeAxes can not be set manually` during overlay initialization so the overlay successfully registers and opens.

## [1.4.5] - 2026-08-22

### Fixed
- **Toolbar Button Tooltip Text**: Explicitly overrode `TooltipText` to return `LazerLensStrings.TooltipMain` ("Lazer Lens") so the button tooltip displays the localized name instead of the C# class name.

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
