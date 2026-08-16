using System;
using System.Collections.Generic;
using System.Linq;

namespace LazerLens.Models
{
    /// <summary>
    /// Aggregates all live statistics for the current play session.
    /// </summary>
    public class SessionState
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset SessionStart { get; set; } = DateTimeOffset.Now;
        public double? InitialProfilePP { get; set; }
        public double? CurrentProfilePP { get; set; }
        public double SessionPPGain => Plays.Sum(p => p.ProfilePerformancePoints ?? 0);

        public List<SessionPlayRecord> Plays { get; } = new();

        public int TotalPlays => Plays.Count;
        public int TotalPasses => Plays.Count(p => p.Passed);
        public int TotalFails => Plays.Count(p => !p.Passed);

        public double AverageAccuracy => Plays.Count == 0 ? 0.0 : Plays.Average(p => p.Accuracy);
        public int MaxCombo => Plays.Count == 0 ? 0 : Plays.Max(p => p.MaxCombo);
        public long TotalScore => Plays.Sum(p => p.TotalScore);

        public SessionPlayRecord? BestScore => Plays
            .Where(p => p.Passed)
            .OrderByDescending(p => p.PerformancePoints ?? 0)
            .ThenByDescending(p => p.TotalScore)
            .FirstOrDefault();

        public TimeSpan SessionDuration => DateTimeOffset.Now - SessionStart;

        public void Reset()
        {
            Id = Guid.NewGuid();
            SessionStart = DateTimeOffset.Now;
            InitialProfilePP = CurrentProfilePP;
            Plays.Clear();
        }
    }
}

