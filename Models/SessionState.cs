using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LazerLens.Models
{
    /// <summary>
    /// Aggregates all live statistics for the current play session.
    /// </summary>
    public class SessionState
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset SessionStart { get; set; } = GetProcessStartTime();
        public DateTimeOffset? SessionEnd { get; set; }
        public double? InitialProfilePP { get; set; }
        public double? CurrentProfilePP { get; set; }
        public double SessionPPGain => Plays.Sum(p => p.ProfilePerformancePoints ?? 0);

        public List<SessionPlayRecord> Plays { get; } = new();

        public int TotalPlays => Plays.Count;
        public int TotalPasses => Plays.Count(p => p.Passed);
        public int TotalFails => Plays.Count(p => !p.Passed);

        public double AverageAccuracy => Plays.Count == 0 ? 0.0 : Plays.Average(p => p.Accuracy);
        public double AverageUnstableRate => Plays.Any(p => p.UnstableRate.HasValue) ? Plays.Where(p => p.UnstableRate.HasValue).Average(p => p.UnstableRate!.Value) : 0.0;
        public int MaxCombo => Plays.Count == 0 ? 0 : Plays.Max(p => p.MaxCombo);
        public long TotalScore => Plays.Sum(p => p.TotalScore);

        public SessionPlayRecord? BestScore => Plays
            .Where(p => p.Passed)
            .OrderByDescending(p => p.PerformancePoints ?? 0)
            .ThenByDescending(p => p.TotalScore)
            .FirstOrDefault();

        public TimeSpan SessionDuration => (SessionEnd ?? DateTimeOffset.Now) - SessionStart;

        public static DateTimeOffset GetProcessStartTime()
        {
            try
            {
                using var proc = Process.GetCurrentProcess();
                return new DateTimeOffset(proc.StartTime);
            }
            catch
            {
                return DateTimeOffset.Now;
            }
        }

        public void Reset()
        {
            Id = Guid.NewGuid();
            SessionStart = DateTimeOffset.Now;
            InitialProfilePP = CurrentProfilePP;
            Plays.Clear();
        }
    }
}
