using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace LazerLens.Models
{
    public class SessionSummary
    {
        public Guid Id { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public int PlayCount { get; set; }
        public double TopPP { get; set; }
        public string? TopScoreTitle { get; set; }
        public double AverageAccuracy { get; set; }
    }

    public class PlayRecordDto
    {
        public Guid Id { get; set; }
        public string BeatmapTitle { get; set; } = string.Empty;
        public string BeatmapArtist { get; set; } = string.Empty;
        public string DifficultyName { get; set; } = string.Empty;
        public string RulesetName { get; set; } = string.Empty;
        public double Accuracy { get; set; }
        public long TotalScore { get; set; }
        public int MaxCombo { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string[] Mods { get; set; } = Array.Empty<string>();
        public bool Passed { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public double StarRating { get; set; }
        public double? PerformancePoints { get; set; }
        public double? ProfilePerformancePoints { get; set; }
        public double TotalPPBeforePlay { get; set; }
        public string Status { get; set; } = "Ranked";
        public int OnlineBeatmapID { get; set; }
        public int OnlineBeatmapSetID { get; set; }
        public string Rank { get; set; } = "A";
        public Dictionary<string, int>? Statistics { get; set; }
        public double? UnstableRate { get; set; }
    }

    public class SessionArchive
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public double? InitialProfilePP { get; set; }
        public double? CurrentProfilePP { get; set; }
        public List<PlayRecordDto> Plays { get; set; } = new();

        public static SessionArchive FromState(SessionState state)
        {
            return new SessionArchive
            {
                Id = state.Id,
                StartTime = state.SessionStart,
                EndTime = DateTimeOffset.Now,
                InitialProfilePP = state.InitialProfilePP,
                CurrentProfilePP = state.CurrentProfilePP,
                Plays = state.Plays.Select(p => new PlayRecordDto
                {
                    Id = p.Id,
                    BeatmapTitle = p.BeatmapTitle,
                    BeatmapArtist = p.BeatmapArtist,
                    DifficultyName = p.DifficultyName,
                    RulesetName = p.RulesetName,
                    Accuracy = p.Accuracy,
                    TotalScore = p.TotalScore,
                    MaxCombo = p.MaxCombo,
                    Grade = p.Grade,
                    Mods = p.Mods,
                    Passed = p.Passed,
                    Timestamp = p.Timestamp,
                    StarRating = p.StarRating,
                    PerformancePoints = p.PerformancePoints,
                    ProfilePerformancePoints = p.ProfilePerformancePoints,
                    TotalPPBeforePlay = p.TotalPPBeforePlay,
                    Status = p.Status,
                    OnlineBeatmapID = p.OnlineBeatmapID,
                    OnlineBeatmapSetID = p.OnlineBeatmapSetID,
                    Rank = p.Rank.ToString(),
                    Statistics = p.Statistics?.ToDictionary(k => k.Key.ToString(), v => v.Value),
                    UnstableRate = p.UnstableRate
                }).ToList()
            };
        }

        public static SessionState ToState(SessionArchive archive)
        {
            var state = new SessionState
            {
                Id = archive.Id,
                SessionStart = archive.StartTime,
                InitialProfilePP = archive.InitialProfilePP,
                CurrentProfilePP = archive.CurrentProfilePP
            };

            foreach (var playDto in archive.Plays)
            {
                var stats = new Dictionary<HitResult, int>();
                if (playDto.Statistics != null)
                {
                    foreach (var kvp in playDto.Statistics)
                    {
                        if (Enum.TryParse<HitResult>(kvp.Key, out var hr))
                        {
                            stats[hr] = kvp.Value;
                        }
                    }
                }

                Enum.TryParse<ScoreRank>(playDto.Rank, out var rank);

                var play = new SessionPlayRecord(
                    playDto.BeatmapTitle,
                    playDto.BeatmapArtist,
                    playDto.DifficultyName,
                    playDto.RulesetName,
                    playDto.Accuracy,
                    playDto.TotalScore,
                    playDto.MaxCombo,
                    playDto.Grade,
                    playDto.Mods,
                    playDto.Passed,
                    playDto.Timestamp,
                    playDto.StarRating,
                    playDto.PerformancePoints,
                    playDto.ProfilePerformancePoints,
                    playDto.TotalPPBeforePlay,
                    playDto.Status,
                    playDto.OnlineBeatmapID,
                    playDto.OnlineBeatmapSetID,
                    rank,
                    stats.Count > 0 ? stats : null,
                    playDto.UnstableRate,
                    playDto.Id
                );
                state.Plays.Add(play);
            }

            return state;
        }

        public static SessionSummary ToSummary(SessionArchive archive)
        {
            var bestScore = archive.Plays
                .Where(p => p.Passed)
                .OrderByDescending(p => p.PerformancePoints ?? 0)
                .ThenByDescending(p => p.TotalScore)
                .FirstOrDefault();

            return new SessionSummary
            {
                Id = archive.Id,
                StartTime = archive.StartTime,
                EndTime = archive.EndTime,
                PlayCount = archive.Plays.Count,
                TopPP = bestScore?.PerformancePoints ?? 0.0,
                TopScoreTitle = bestScore != null ? $"{bestScore.BeatmapArtist} - {bestScore.BeatmapTitle} [{bestScore.DifficultyName}]" : null,
                AverageAccuracy = archive.Plays.Count == 0 ? 0.0 : archive.Plays.Average(p => p.Accuracy)
            };
        }
    }
}

