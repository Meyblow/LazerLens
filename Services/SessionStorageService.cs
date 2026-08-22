using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using osu.Framework.Platform;
using osucc.Core;
using LazerLens.Models;

namespace LazerLens.Services
{
    public class SessionStorageService
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = false };

        private readonly Storage? _baseStorage;
        private readonly Storage? _sessionsStorage;
        private List<SessionSummary>? _cachedSummaries;

        public SessionStorageService(Storage? storage)
        {
            _baseStorage = storage;

            if (storage != null)
            {
                try
                {
                    string pluginFullPath = storage.GetFullPath(string.Empty);
                    DirectoryInfo? pluginDir = new DirectoryInfo(pluginFullPath);
                    DirectoryInfo? osuCcDir = pluginDir.Parent?.Parent;

                    if (osuCcDir != null && osuCcDir.Exists)
                    {
                        string sessionsPath = Path.Combine(osuCcDir.FullName, "sessions");
                        _sessionsStorage = new NativeStorage(sessionsPath);
                    }
                    else
                    {
                        string sessionsPath = Path.GetFullPath(Path.Combine(pluginFullPath, "..", "..", "sessions"));
                        _sessionsStorage = new NativeStorage(sessionsPath);
                    }
                }
                catch (Exception ex)
                {
                    TimingLog.Error($"SessionStorageService: failed to resolve osu-cc/sessions storage: {ex}");
                    _sessionsStorage = storage.GetStorageForDirectory("sessions");
                }
            }

            migrateLegacySessions(storage);
        }

        private void migrateLegacySessions(Storage? storage)
        {
            if (storage == null || _sessionsStorage == null) return;

            try
            {
                var legacyStorage = storage.GetStorageForDirectory("sessions");
                var legacyFiles = legacyStorage.GetFiles(".", "*.json");

                foreach (var file in legacyFiles)
                {
                    try
                    {
                        if (!_sessionsStorage.Exists(file))
                        {
                            using var src = legacyStorage.GetStream(file);
                            using var dst = _sessionsStorage.GetStream(file, FileAccess.Write, FileMode.Create);
                            src.CopyTo(dst);
                        }
                    }
                    catch (Exception ex)
                    {
                        TimingLog.Error($"Failed to migrate session file {file}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed during legacy session migration: {ex}");
            }
        }

        public void SaveSession(SessionState state)
        {
            if (_sessionsStorage == null) return;

            try
            {
                var archive = SessionArchive.FromState(state);
                var timestamp = archive.StartTime.ToUnixTimeSeconds();
                var fileName = $"{timestamp}_{archive.Id}.json";

                using var stream = _sessionsStorage.GetStream(fileName, FileAccess.Write, FileMode.Create);
                JsonSerializer.Serialize(stream, archive, s_jsonOptions);

                InvalidateCache();
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed to save session: {ex.Message}");
            }
        }

        public List<SessionSummary> GetAllSessions()
        {
            if (_cachedSummaries != null)
                return _cachedSummaries;

            var summaries = new List<SessionSummary>();
            if (_sessionsStorage == null) return summaries;

            try
            {
                var files = _sessionsStorage.GetFiles(".", "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        using var stream = _sessionsStorage.GetStream(file);
                        var archive = JsonSerializer.Deserialize<SessionArchive>(stream);
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

                _cachedSummaries = summaries.OrderByDescending(s => s.StartTime).ToList();
                return _cachedSummaries;
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed to get all sessions: {ex.Message}");
                return new List<SessionSummary>();
            }
        }

        public SessionState? LoadSession(Guid sessionId)
        {
            if (_sessionsStorage == null) return null;

            try
            {
                var files = _sessionsStorage.GetFiles(".", $"*_{sessionId}.json");
                var file = files.FirstOrDefault();

                if (file == null) return null;

                using var stream = _sessionsStorage.GetStream(file);
                var archive = JsonSerializer.Deserialize<SessionArchive>(stream);

                if (archive == null) return null;

                return SessionArchive.ToState(archive);
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed to load session {sessionId}: {ex.Message}");
                return null;
            }
        }

        public void DeleteSession(Guid sessionId)
        {
            if (_sessionsStorage == null) return;

            try
            {
                var files = _sessionsStorage.GetFiles(".", $"*_{sessionId}.json");
                var file = files.FirstOrDefault();

                if (file != null)
                {
                    _sessionsStorage.Delete(file);
                    InvalidateCache();
                }
            }
            catch (Exception ex)
            {
                TimingLog.Error($"Failed to delete session {sessionId}: {ex.Message}");
            }
        }

        public void InvalidateCache()
        {
            _cachedSummaries = null;
        }

        public void OpenSessionsDirectory()
        {
            _sessionsStorage?.PresentExternally();
        }

        public string? ExportToCsv()
        {
            try
            {
                var desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (string.IsNullOrEmpty(desktopPath))
                    return null;

                var exportFolder = Path.Combine(desktopPath, "osu_session_exports");
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
    }
}

