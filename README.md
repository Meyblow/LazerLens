# Lazer Lens

osu!cc plugin that tracks your session stats in-game. Playcount, average accuracy,
PP gained today, play history — all without alt-tabbing to some website.

Made this for myself because checking osu!stats after every session got annoying fast.

### Features

**Session overlay**
Click the toolbar button to open the panel. Shows total plays, average acc, and
your top PP score for the current session. Updates live as you play.

**Play history**
Scrollable list of everything you played since launching the game — passed maps,
retries, and fails. Each entry shows acc, grade, mods, PP and UR if you care
about it.

**Session archive**
Every session is saved locally as a JSON file. Pick any past session from the
archive tab to review it with full stats and play history. You can also open the sessions folder directly from settings.

**CSV export**
One button dumps your entire session history to a `.csv` in `~/Downloads/osu_session_exports/`.
Useful if you want to track long-term progress in a spreadsheet.

**Settings**
- Compact list mode (smaller rows, more plays visible)
- Toggle UR visibility
- Toggle in-game notifications on each recorded play
- Enable/disable retry tracking and fails

### Install

1. Go to the [Releases](https://github.com/Meyblow/LazerLens/releases) tab
2. Download the latest `lazer-lens.zip`
3. Drop it into your osu-cc plugins folder — `%APPDATA%\osu\osu-cc\plugins`
4. Restart the game, then `Ctrl+O` → Specials to configure

### Notes

- Sessions are stored per-day. If you restart the game mid-day, the existing
  session for that day gets resumed.
- Replay and spectator plays are intentionally ignored.
- Requires osu!cc with plugin support.

---
**Meyblow** — [Telegram](https://t.me/Meyblow) · [osu! profile](https://osu.ppy.sh/users/39791134)
