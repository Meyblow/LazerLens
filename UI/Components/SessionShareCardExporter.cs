using System;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Platform;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osucc.Core;
using LazerLens.Models;
using LazerLens.Services;
using LazerLens.Utilities;

namespace LazerLens.UI.Components
{
    public static class SessionShareCardExporter
    {
        public static void ExportAndShare(SessionState session, Clipboard? clipboard = null, NotificationOverlay? notifications = null)
        {
            try
            {
                if (session == null || session.TotalPlays == 0)
                {
                    notifications?.Post(new SimpleNotification
                    {
                        Text = "No plays in this session to share."
                    });
                    return;
                }

                // Generate rich text summary formatted for Discord / Socials
                var sb = new StringBuilder();
                sb.AppendLine($"📊 **osu! LazerLens Session Report** — {session.SessionStart:dd MMM yyyy}");
                sb.AppendLine($"⏱ Duration: {session.SessionDuration.Hours:00}:{session.SessionDuration.Minutes:00}:{session.SessionDuration.Seconds:00}");
                sb.AppendLine($"🎯 Plays: {session.TotalPlays} ({session.TotalPasses} Pass / {session.TotalFails} Fail)");
                sb.AppendLine($"📈 Session PP: {session.SessionPPGain:+0.0;-0.0;0.0} pp");
                sb.AppendLine($"🎯 Avg Accuracy: {session.AverageAccuracy:F2}%");
                sb.AppendLine($"🔥 Max Combo: {session.MaxCombo}x");
                if (session.AverageUnstableRate > 0)
                    sb.AppendLine($"⚡ Avg UR: {session.AverageUnstableRate:F1}");

                var topScores = session.Plays
                    .Where(p => p.Passed)
                    .OrderByDescending(p => p.PerformancePoints ?? 0)
                    .Take(3)
                    .ToList();

                if (topScores.Count > 0)
                {
                    sb.AppendLine("\n🏆 **Top Scores:**");
                    for (int i = 0; i < topScores.Count; i++)
                    {
                        var s = topScores[i];
                        string pp = s.PerformancePoints.HasValue && s.PerformancePoints.Value > 0 ? $"{s.PerformancePoints.Value:F0}pp" : $"{s.TotalScore:N0} pts";
                        string mods = s.Mods.Length > 0 ? $" +{string.Join("", s.Mods)}" : "";
                        sb.AppendLine($"{i + 1}. **{s.BeatmapArtist} - {s.BeatmapTitle} [{s.DifficultyName}]**{mods} — {s.Grade} ({s.Accuracy:F2}%) • {pp}");
                    }
                }

                string reportText = sb.ToString();

                // Copy to system clipboard
                if (clipboard != null)
                {
                    clipboard.SetText(reportText);
                }

                // Save report to file
                string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "osu_session_exports");
                if (!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);

                string filePath = Path.Combine(exportDir, $"session_{session.SessionStart:yyyyMMdd_HHmmss}.txt");
                File.WriteAllText(filePath, reportText, Encoding.UTF8);

                notifications?.Post(new SimpleNotification
                {
                    Text = LazerLensStrings.ShareSessionSuccess.ToString(),
                    Icon = osu.Framework.Graphics.Sprites.FontAwesome.Solid.ShareAlt
                });
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Share session failed: {ex.Message}");
                notifications?.Post(new SimpleNotification
                {
                    Text = LazerLensStrings.ShareSessionFailed.ToString()
                });
            }
        }
    }
}
