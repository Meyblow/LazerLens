using System;

namespace LazerLens.Models
{
    public class SessionGoal
    {
        public SessionGoalType Type { get; set; } = SessionGoalType.None;
        public double TargetValue { get; set; } = 0.0;
        public bool IsAchieved { get; set; } = false;

        public double GetProgress(SessionState session)
        {
            if (session == null || Type == SessionGoalType.None || TargetValue <= 0)
                return 0.0;

            double current = Type switch
            {
                SessionGoalType.PlayCount => session.TotalPlays,
                SessionGoalType.PpGain => Math.Max(0, session.SessionPPGain),
                SessionGoalType.Accuracy => session.AverageAccuracy,
                _ => 0.0
            };

            return Math.Clamp(current / TargetValue, 0.0, 1.0);
        }

        public string GetProgressString(SessionState session)
        {
            if (session == null || Type == SessionGoalType.None || TargetValue <= 0)
                return string.Empty;

            return Type switch
            {
                SessionGoalType.PlayCount => $"{session.TotalPlays} / {TargetValue:F0} plays",
                SessionGoalType.PpGain => $"{session.SessionPPGain:+0.0;-0.0;0.0} / +{TargetValue:F1} pp",
                SessionGoalType.Accuracy => $"{session.AverageAccuracy:F2}% / {TargetValue:F2}%",
                _ => string.Empty
            };
        }

        public string GetTargetDisplayString()
        {
            return Type switch
            {
                SessionGoalType.PlayCount => $"{TargetValue:F0} plays",
                SessionGoalType.PpGain => $"+{TargetValue:F1} PP",
                SessionGoalType.Accuracy => $"{TargetValue:F2}% acc",
                _ => string.Empty
            };
        }

        public bool CheckAchieved(SessionState session)
        {
            if (session == null || Type == SessionGoalType.None || TargetValue <= 0 || IsAchieved)
                return false;

            if (GetProgress(session) >= 1.0)
            {
                IsAchieved = true;
                return true;
            }

            return false;
        }
    }
}
