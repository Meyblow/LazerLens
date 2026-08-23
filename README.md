# 🔍 Lazer Lens

[English](README.md) | [Русский](README.ru.md)

An in-game session tracker, analytics dashboard, and play history overlay for **osu!cc** and **osu!lazer**. Track your play count, average accuracy, PP gains, streaks, and session goals in real time without alt-tabbing to external websites.

---

## ✨ Features

### 🎮 Live Session Tracking
Monitor your current gameplay session with live-updating metrics:
* **Session Stats**: Total plays, pass/fail counts, average accuracy, session PP gain, top score, and max combo.
* **Session Target Goal**: Set custom targets for play count, PP gain, or average accuracy with real-time progress indicators.
* **Session Progression Graph**: Visualize PP gain, accuracy %, and star difficulty progression across your session plays.
* **Interactive Play History**: Instant breakdown of every map played with accuracy, grade, UR, mods, combo, and difficulty tags.

![Live Session Overview](https://raw.githubusercontent.com/Meyblow/LazerLens/main/assets/screenshots/session_tab_overview.png)

---

### 📂 Session Archive & History
Every play session is automatically preserved locally as structured JSON data:
* **Saved Sessions List**: Browse, pin, rename, add notes to, or delete past sessions.
* **Multi-Filter Engine**: Filter plays by Ruleset (`osu!`, `osu!taiko`, `osu!catch`, `osu!mania`), Outcome (`Pass`, `Fail`), Status (`Ranked`, `Loved`, `Graveyard`), or search by beatmap title/artist.
* **Session Best Trophy**: Highlights your top performance of the archived session.
* **Export Options**: Export session data to CSV or copy formatted shareable summaries.

![Past Sessions Archive](https://raw.githubusercontent.com/Meyblow/LazerLens/main/assets/screenshots/archive_tab_history.png)

---

### 📊 Global Analytics & Heatmap
Long-term performance analytics across your entire play history:
* **Annual Activity Heatmap**: 52-week GitHub-style activity grid tracking daily plays, current streak, best streak, and total active days.
* **Cumulative PP Growth Timeline**: Chronological PP progression starting from an honest 0 PP baseline with interactive per-ruleset filtering (`All`, `osu!`, `osu!taiko`, `osu!catch`, `osu!mania`).
* **Mod Usage & Star Rating Breakdown**: Visual distributions of your most played mods and star difficulty spread.
* **Top Beatmaps & Mappers**: Leaderboard of your most played songs and favorite creators.

![Analytics Dashboard](https://raw.githubusercontent.com/Meyblow/LazerLens/main/assets/screenshots/analytics_tab.png)

---

### 🎯 Session Target Goals
Set session goals to stay motivated and track your improvement:

![Session Goal Setup](https://raw.githubusercontent.com/Meyblow/LazerLens/main/assets/screenshots/session_goal_modal_dropdown.png)

---

### ⚙️ Customization & Settings
Fine-tune how metrics and history are calculated and presented:
* Default history sorting (Recent, Score, Accuracy, PP, Combo, Grade, Difficulty).
* PP display mode (Delta `+PP`, Total `Profile PP`, or both).
* Average accuracy calculation method (Weighted by hit objects or simple average).
* Customizable session split threshold (By Day, Midnight, or custom idle duration).
* Unstable Rate (UR) highlight and difficulty badges.

![Settings](https://raw.githubusercontent.com/Meyblow/LazerLens/main/assets/screenshots/settings_tab.png)

---

## 📦 Installation

1. Go to the [Releases](https://github.com/Meyblow/LazerLens/releases) tab.
2. Download `LazerLens.dll` (or `plugin-lazer-lens.zip`).
3. Place the file into your osu!cc plugins directory:
   * **Windows**: `%APPDATA%\osu\osu-cc\plugins\lazer-lens\`
   * **Linux / macOS**: `~/.local/share/osu/osu-cc/plugins/lazer-lens/`
4. Launch osu!cc and click the **Lazer Lens** chart icon in the toolbar.

---

## 🛠️ Building from Source

```bash
dotnet build -c Release
```

---

## 👤 Author

**Meyblow** — [Telegram](https://t.me/Meyblow) · [osu! profile](https://osu.ppy.sh/users/39791134)

