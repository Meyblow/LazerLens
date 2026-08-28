using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using osucc.Core;
using osucc.Data;
using LazerLens.Models;
using LazerLens.Utilities;

namespace LazerLens.Services
{
    public class SessionStorageService
    {
        private const string sessions_directory = "sessions";

        private readonly IOsuCcStorage? _storage;
        private readonly object storageLock = new();
        private static DateTimeOffset? lastPruneTimestamp;
        private List<SessionSummary>? _cachedSummaries;

        public SessionStorageService(IOsuCcStorage? storage)
        {
            _storage = storage;
            migrateLegacySessions();
        }

        public void SaveSession(SessionState state)
        {
            if (_storage == null) return;

            lock (storageLock)
            {
                try
                {
                    var archive = SessionArchive.FromState(state);
                    var timestamp = archive.StartTime.ToUnixTimeSeconds();
                    var fileName = $"{timestamp}_{archive.Id}.json";

                    _storage.WriteJson($"{sessions_directory}/{fileName}", archive);
                    InvalidateCache();
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"Failed to save session: {ex.Message}");
                }
            }
        }

        public List<SessionSummary> GetAllSessions()
        {
            lock (storageLock)
            {
                if (_cachedSummaries != null)
                    return _cachedSummaries;

                var summaries = new List<SessionSummary>();
                if (_storage == null) return summaries;

                try
                {
                    var files = _storage.GetFiles(sessions_directory, "*.json");
                    foreach (var file in files)
                    {
                        try
                        {
                            var path = file.StartsWith(sessions_directory, StringComparison.OrdinalIgnoreCase)
                                ? file
                                : $"{sessions_directory}/{file}";

                            var archive = _storage.ReadJson<SessionArchive>(path);
                            if (archive != null)
                            {
                                summaries.Add(SessionArchive.ToSummary(archive));
                            }
                        }
                        catch (Exception ex)
                        {
                            TimingLog.Error($"Failed to load session file {file}: {ex.Message}");
                        }
                    }

                    _cachedSummaries = summaries.OrderByDescending(s => s.IsPinned).ThenByDescending(s => s.StartTime).ToList();
                    return _cachedSummaries;
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"Failed to get all sessions: {ex.Message}");
                    return new List<SessionSummary>();
                }
            }
        }

        public SessionState? LoadSession(Guid sessionId)
        {
            if (_storage == null) return null;

            lock (storageLock)
            {
                try
                {
                    var files = _storage.GetFiles(sessions_directory, $"*_{sessionId}.json");
                    var file = files.FirstOrDefault();

                    if (file == null) return null;

                    var path = file.StartsWith(sessions_directory, StringComparison.OrdinalIgnoreCase)
                        ? file
                        : $"{sessions_directory}/{file}";

                    var archive = _storage.ReadJson<SessionArchive>(path);
                    if (archive == null) return null;

                    return SessionArchive.ToState(archive);
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"Failed to load session {sessionId}: {ex.Message}");
                    return null;
                }
            }
        }

        public void SetSessionPinned(Guid sessionId, bool pinned)
        {
            if (_storage == null) return;

            lock (storageLock)
            {
                try
                {
                    var files = _storage.GetFiles(sessions_directory, $"*_{sessionId}.json");
                    var file = files.FirstOrDefault();

                    if (file == null) return;

                    var path = file.StartsWith(sessions_directory, StringComparison.OrdinalIgnoreCase)
                        ? file
                        : $"{sessions_directory}/{file}";

                    var archive = _storage.ReadJson<SessionArchive>(path);
                    if (archive != null)
                    {
                        archive.IsPinned = pinned;
                        _storage.WriteJson(path, archive);
                        InvalidateCache();
                    }
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"Failed to set pinned for session {sessionId}: {ex.Message}");
                }
            }
        }

        public void SetSessionNote(Guid sessionId, string? note)
        {
            if (_storage == null) return;

            lock (storageLock)
            {
                try
                {
                    var files = _storage.GetFiles(sessions_directory, $"*_{sessionId}.json");
                    var file = files.FirstOrDefault();

                    if (file == null) return;

                    var path = file.StartsWith(sessions_directory, StringComparison.OrdinalIgnoreCase)
                        ? file
                        : $"{sessions_directory}/{file}";

                    var archive = _storage.ReadJson<SessionArchive>(path);
                    if (archive != null)
                    {
                        archive.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                        _storage.WriteJson(path, archive);
                        InvalidateCache();
                    }
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"Failed to set note for session {sessionId}: {ex.Message}");
                }
            }
        }

        public void OpenSessionFile(Guid sessionId)
        {
            if (_storage == null) return;

            try
            {
                var files = _storage.GetFiles(sessions_directory, $"*_{sessionId}.json");
                var file = files.FirstOrDefault();

                if (file != null)
                {
                    var path = file.StartsWith(sessions_directory, StringComparison.OrdinalIgnoreCase)
                        ? file
                        : $"{sessions_directory}/{file}";

                    var fullPath = _storage.GetFullPath(path);
                    if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                    {
                        PlatformHelper.OpenAndSelectFile(fullPath);
                        return;
                    }
                }

                OpenSessionsDirectory();
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed to open session file {sessionId}: {ex.Message}");
                OpenSessionsDirectory();
            }
        }

        public void DeleteSession(Guid sessionId)
        {
            if (_storage == null) return;

            lock (storageLock)
            {
                try
                {
                    var files = _storage.GetFiles(sessions_directory, $"*_{sessionId}.json");
                    foreach (var file in files)
                    {
                        var path = file.StartsWith(sessions_directory, StringComparison.OrdinalIgnoreCase)
                            ? file
                            : $"{sessions_directory}/{file}";

                        var fullPath = _storage.GetFullPath(path);
                        if (!string.IsNullOrEmpty(fullPath) && File.Exists(fullPath))
                        {
                            File.Delete(fullPath);
                            InvalidateCache();
                        }
                    }
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"Failed to delete session {sessionId}: {ex.Message}");
                }
            }
        }

        public void InvalidateCache()
        {
            lock (storageLock)
            {
                _cachedSummaries = null;
            }
        }

        public void OpenSessionsDirectory()
        {
            var fullPath = _storage?.GetFullPath(sessions_directory);
            if (!string.IsNullOrEmpty(fullPath))
            {
                PlatformHelper.OpenDirectory(fullPath);
            }
        }

        public string? ExportToCsv()
        {
            try
            {
                string? downloadsPath = null;
                try
                {
                    downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    if (!Directory.Exists(downloadsPath))
                    {
                        downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(downloadsPath) || !Directory.Exists(downloadsPath))
                {
                    downloadsPath = _storage?.GetFullPath("exports") ?? Path.GetTempPath();
                }

                var exportFolder = Path.Combine(downloadsPath, "osu_session_exports");
                if (!Directory.Exists(exportFolder))
                    Directory.CreateDirectory(exportFolder);

                var fileName = $"sessions_export_{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}.csv";
                var fullPath = Path.Combine(exportFolder, fileName);

                using var writer = new StreamWriter(fullPath);
                writer.WriteLine("Session ID,Start Time,End Time,Total Plays,Average Accuracy,Top PP,Top Score");

                var summaries = GetAllSessions();
                foreach (var s in summaries.OrderByDescending(x => x.StartTime))
                {
                    string safeTopScoreTitle = s.TopScoreTitle?.Replace(",", "") ?? "";
                    writer.WriteLine($"{s.Id},{s.StartTime:yyyy-MM-dd HH:mm:ss},{s.EndTime:yyyy-MM-dd HH:mm:ss},{s.PlayCount},{s.AverageAccuracy.ToString("F2", CultureInfo.InvariantCulture)},{s.TopPP.ToString("F2", CultureInfo.InvariantCulture)},{safeTopScoreTitle}");
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed to export sessions to CSV: {ex.Message}");
                return null;
            }
        }

        public string? ExportSingleSessionToCsv(Guid sessionId)
        {
            try
            {
                var session = LoadSession(sessionId);
                if (session == null)
                    return null;

                string? downloadsPath = null;
                try
                {
                    downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    if (!Directory.Exists(downloadsPath))
                    {
                        downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(downloadsPath) || !Directory.Exists(downloadsPath))
                {
                    downloadsPath = _storage?.GetFullPath("exports") ?? Path.GetTempPath();
                }

                var exportFolder = Path.Combine(downloadsPath, "osu_session_exports");
                if (!Directory.Exists(exportFolder))
                    Directory.CreateDirectory(exportFolder);

                var fileName = $"session_{session.SessionStart:yyyy-MM-dd_HH-mm-ss}_{session.Id}.csv";
                var fullPath = Path.Combine(exportFolder, fileName);

                using var writer = new StreamWriter(fullPath);
                writer.WriteLine("Timestamp,Artist,Title,Difficulty,Mapper,Ruleset,Grade,Accuracy,Score,MaxCombo,Stars,RawPP,ProfilePP,Mods,Passed,UnstableRate,Status");

                foreach (var p in session.Plays.OrderBy(x => x.Timestamp))
                {
                    string safeArtist = p.BeatmapArtist?.Replace(",", " ") ?? "";
                    string safeTitle = p.BeatmapTitle?.Replace(",", " ") ?? "";
                    string safeDiff = p.DifficultyName?.Replace(",", " ") ?? "";
                    string safeMapper = p.BeatmapMapper?.Replace(",", " ") ?? "";
                    string safeMods = p.Mods != null && p.Mods.Length > 0 ? string.Join("+", p.Mods) : "NoMod";
                    string rawPp = p.PerformancePoints.HasValue ? p.PerformancePoints.Value.ToString("F2", CultureInfo.InvariantCulture) : "0.00";
                    string profPp = p.ProfilePerformancePoints.HasValue ? p.ProfilePerformancePoints.Value.ToString("F2", CultureInfo.InvariantCulture) : "0.00";
                    string ur = p.UnstableRate.HasValue ? p.UnstableRate.Value.ToString("F2", CultureInfo.InvariantCulture) : "";

                    writer.WriteLine($"{p.Timestamp:yyyy-MM-dd HH:mm:ss},{safeArtist},{safeTitle},{safeDiff},{safeMapper},{p.RulesetName},{p.Grade},{p.Accuracy.ToString("F2", CultureInfo.InvariantCulture)},{p.TotalScore},{p.MaxCombo},{p.StarRating.ToString("F2", CultureInfo.InvariantCulture)},{rawPp},{profPp},{safeMods},{p.Passed},{ur},{p.Status}");
                }

                return fullPath;
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed to export single session to CSV: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Automatically migrates sessions from legacy directories into the VFS directory osu-cc/data/lazer-lens/sessions/.
        /// </summary>
        private void migrateLegacySessions()
        {
            if (_storage == null) return;

            try
            {
                string? targetDir = _storage.GetFullPath(sessions_directory);
                if (string.IsNullOrEmpty(targetDir)) return;

                if (!Directory.Exists(targetDir))
                    Directory.CreateDirectory(targetDir);

                var targetDirInfo = new DirectoryInfo(targetDir);
                var osuCcDir = targetDirInfo.Parent?.Parent?.Parent; // <root>/osu-cc

                if (osuCcDir != null && osuCcDir.Exists)
                {
                    var legacyCandidates = new[]
                    {
                        Path.Combine(osuCcDir.FullName, "plugins", "lazer-lens", "sessions"),
                        Path.Combine(osuCcDir.FullName, "plugins", "LazerLens", "sessions"),
                        Path.Combine(osuCcDir.FullName, "sessions"),
                    };

                    foreach (var legacyDir in legacyCandidates)
                    {
                        if (Directory.Exists(legacyDir) && !string.Equals(legacyDir, targetDir, StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (var file in Directory.GetFiles(legacyDir, "*.json"))
                            {
                                var destFile = Path.Combine(targetDir, Path.GetFileName(file));
                                if (!File.Exists(destFile))
                                {
                                    File.Copy(file, destFile);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed during legacy session migration: {ex}");
            }
        }

        public void PruneOldSessions(ArchiveRetentionLimit limit)
        {
            if (_storage == null || limit == ArchiveRetentionLimit.Unlimited) return;

            lock (storageLock)
            {
                var now = DateTimeOffset.Now;

                // Throttling: execute at most once every 24 hours
                if (lastPruneTimestamp.HasValue && (now - lastPruneTimestamp.Value).TotalHours < 24)
                    return;

                lastPruneTimestamp = now;

                try
                {
                    var summaries = GetAllSessions().Where(s => !s.IsPinned).ToList();
                    var toDelete = new List<SessionSummary>();

                    switch (limit)
                    {
                        case ArchiveRetentionLimit.ThirtyDays:
                            toDelete = summaries.Where(s => (now - s.StartTime).TotalDays > 30).ToList();
                            break;

                        case ArchiveRetentionLimit.NinetyDays:
                            toDelete = summaries.Where(s => (now - s.StartTime).TotalDays > 90).ToList();
                            break;

                        case ArchiveRetentionLimit.OneHundredSessions:
                            if (summaries.Count > 100)
                                toDelete = summaries.OrderByDescending(s => s.StartTime).Skip(100).ToList();
                            break;
                    }

                    foreach (var s in toDelete)
                    {
                        DeleteSession(s.Id);
                    }
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"Failed to prune old sessions: {ex.Message}");
                }
            }
        }
    }
}

