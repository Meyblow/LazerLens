using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;

namespace LazerLens.Models
{
    /// <summary>
    /// Represents a single recorded play during the session.
    /// </summary>
    public record SessionPlayRecord(
        string BeatmapTitle,
        string BeatmapArtist,
        string DifficultyName,
        string RulesetName,
        double Accuracy,
        long TotalScore,
        int MaxCombo,
        string Grade,
        string[] Mods,
        bool Passed,
        DateTimeOffset Timestamp,
        double StarRating = 0.0,
        double? PerformancePoints = null,
        double? ProfilePerformancePoints = null,
        double TotalPPBeforePlay = 0.0,
        string Status = "Ranked",
        int OnlineBeatmapID = 0,
        int OnlineBeatmapSetID = 0,
        ScoreRank Rank = ScoreRank.A,
        IReadOnlyDictionary<HitResult, int>? Statistics = null,
        double? UnstableRate = null,
        double? IfFcPerformancePoints = null,
        bool IsChoke = false,
        bool IsWarmup = false,
        Guid Id = default
    )
    {
        public Guid Id { get; init; } = Id == default ? Guid.NewGuid() : Id;

        public int CountGreat => Statistics != null && Statistics.TryGetValue(HitResult.Great, out int val) ? val : 0;
        public int CountOk => Statistics != null && Statistics.TryGetValue(HitResult.Ok, out int val) ? val : 0;
        public int CountMeh => Statistics != null && Statistics.TryGetValue(HitResult.Meh, out int val) ? val : 0;
        public int CountMiss => Statistics != null && Statistics.TryGetValue(HitResult.Miss, out int val) ? val : 0;
        public int CountPerfect => Statistics != null && Statistics.TryGetValue(HitResult.Perfect, out int val) ? val : 0;
        public int CountGood => Statistics != null && Statistics.TryGetValue(HitResult.Good, out int val) ? val : 0;
        public int CountLargeTickHit => Statistics != null && Statistics.TryGetValue(HitResult.LargeTickHit, out int val) ? val : 0;
        public int CountSmallTickHit => Statistics != null && Statistics.TryGetValue(HitResult.SmallTickHit, out int val) ? val : 0;
        public int CountLargeTickMiss => Statistics != null && Statistics.TryGetValue(HitResult.LargeTickMiss, out int val) ? val : 0;
    }
}

