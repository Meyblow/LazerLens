using System;
using System.Collections.Generic;
using osu.Game.Scoring;

namespace LazerLens.Models
{
    public class GlobalAnalyticsData
    {
        public int TotalSessions { get; set; }
        public int TotalPlays { get; set; }
        public int TotalPasses { get; set; }
        public int TotalFails { get; set; }
        public double AverageAccuracy { get; set; }
        public double TotalPpGain { get; set; }
        public double PeakSessionPp { get; set; }
        public TimeSpan TotalPlayTime { get; set; }

        public int CurrentStreakDays { get; set; }
        public int MaxStreakDays { get; set; }
        public int TotalActiveDays { get; set; }
        public DateTime? MostActiveDate { get; set; }
        public int MostActiveDayPlayCount { get; set; }

        public Dictionary<DateTime, int> DayPlayCounts { get; set; } = new();
        public List<SessionTimelineEntry> PpTimeline { get; set; } = new();
        public Dictionary<string, int> ModPlayCounts { get; set; } = new();
        public Dictionary<string, int> StarRatingBuckets { get; set; } = new();
        public List<TopBeatmapStat> TopBeatmaps { get; set; } = new();
        public List<TopMapperStat> TopMappers { get; set; } = new();
    }

    public class SessionTimelineEntry
    {
        public DateTime Date { get; set; }
        public double CumulativePp { get; set; }
        public double SessionAccuracy { get; set; }
        public double SessionPpGain { get; set; }
        public int PlayCount { get; set; }
        public string SessionTitle { get; set; } = string.Empty;
    }

    public class TopBeatmapStat
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Mapper { get; set; } = string.Empty;
        public int PlayCount { get; set; }
        public double StarRating { get; set; }
        public double BestAccuracy { get; set; }
        public double? BestPp { get; set; }
        public int MaxCombo { get; set; }
        public ScoreRank BestGrade { get; set; }
        public int OnlineBeatmapID { get; set; }
    }

    public class TopMapperStat
    {
        public string MapperName { get; set; } = string.Empty;
        public int PlayCount { get; set; }
        public double AverageAccuracy { get; set; }
        public int MapsCount { get; set; }
        public double TopPp { get; set; }
    }
}
