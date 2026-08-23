using System;
using System.Collections.Generic;
using System.Linq;
using LazerLens.Models;

namespace LazerLens.Services
{
    public static class LazerLensAnalyticsEngine
    {
        public static GlobalAnalyticsData BuildAnalytics(IReadOnlyList<SessionSummary> summaries, SessionStorageService? storageService, SessionState? liveState)
        {
            var data = new GlobalAnalyticsData();

            var allPlays = new List<SessionPlayRecord>();

            // 1. Gather all plays from saved sessions
            if (storageService != null && summaries != null)
            {
                foreach (var summary in summaries)
                {
                    var sessionState = storageService.LoadSession(summary.Id);
                    if (sessionState != null && sessionState.Plays.Count > 0)
                    {
                        allPlays.AddRange(sessionState.Plays);
                    }
                }
            }

            // 2. Add current live session plays
            if (liveState != null && liveState.Plays.Count > 0)
            {
                allPlays.AddRange(liveState.Plays);
            }

            data.TotalSessions = (summaries?.Count ?? 0) + (liveState != null && liveState.TotalPlays > 0 ? 1 : 0);
            data.TotalPlays = allPlays.Count;
            data.TotalPasses = allPlays.Count(p => p.Passed);
            data.TotalFails = allPlays.Count(p => !p.Passed);

            if (data.TotalPlays > 0)
            {
                data.AverageAccuracy = allPlays.Average(p => p.Accuracy);
                data.TotalPpGain = allPlays.Where(p => p.PerformancePoints.HasValue).Sum(p => p.PerformancePoints!.Value);
                data.PeakSessionPp = summaries != null && summaries.Count > 0 ? summaries.Max(s => s.TopPP) : 0;
                if (liveState != null && liveState.BestScore?.PerformancePoints > data.PeakSessionPp)
                    data.PeakSessionPp = liveState.BestScore.PerformancePoints.Value;
            }

            // 3. Day Play Counts & Streaks (Last 365 Days / 52 Weeks)
            var dayMap = new Dictionary<DateTime, int>();
            foreach (var play in allPlays)
            {
                var day = play.Timestamp.ToLocalTime().Date;
                if (!dayMap.ContainsKey(day))
                    dayMap[day] = 0;
                dayMap[day]++;
            }

            data.DayPlayCounts = dayMap;
            data.TotalActiveDays = dayMap.Count;

            if (dayMap.Count > 0)
            {
                var peakDay = dayMap.OrderByDescending(kvp => kvp.Value).First();
                data.MostActiveDate = peakDay.Key;
                data.MostActiveDayPlayCount = peakDay.Value;

                // Calculate streaks
                var sortedDates = dayMap.Keys.OrderBy(d => d).ToList();
                int currentStreak = 0;
                int maxStreak = 0;
                int tempStreak = 0;
                DateTime? prevDate = null;

                DateTime today = DateTime.Now.Date;
                DateTime yesterday = today.AddDays(-1);

                foreach (var d in sortedDates)
                {
                    if (prevDate == null || d == prevDate.Value.AddDays(1))
                    {
                        tempStreak++;
                    }
                    else
                    {
                        tempStreak = 1;
                    }

                    if (tempStreak > maxStreak)
                        maxStreak = tempStreak;

                    prevDate = d;
                }

                // Check active current streak
                if (dayMap.ContainsKey(today))
                {
                    currentStreak = 1;
                    DateTime check = today.AddDays(-1);
                    while (dayMap.ContainsKey(check))
                    {
                        currentStreak++;
                        check = check.AddDays(-1);
                    }
                }
                else if (dayMap.ContainsKey(yesterday))
                {
                    currentStreak = 1;
                    DateTime check = yesterday.AddDays(-1);
                    while (dayMap.ContainsKey(check))
                    {
                        currentStreak++;
                        check = check.AddDays(-1);
                    }
                }

                data.CurrentStreakDays = currentStreak;
                data.MaxStreakDays = Math.Max(maxStreak, currentStreak);
            }

            // 4. Mod Breakdown
            var modCounts = new Dictionary<string, int>
            {
                { "NoMod", 0 },
                { "DT / NC", 0 },
                { "HR", 0 },
                { "HD", 0 },
                { "FL", 0 },
                { "EZ / HT", 0 },
                { "Other", 0 }
            };

            foreach (var play in allPlays)
            {
                if (play.Mods == null || play.Mods.Length == 0)
                {
                    modCounts["NoMod"]++;
                    continue;
                }

                string joined = string.Join(",", play.Mods).ToUpperInvariant();
                if (joined.Contains("DT") || joined.Contains("NC")) modCounts["DT / NC"]++;
                else if (joined.Contains("HR")) modCounts["HR"]++;
                else if (joined.Contains("HD")) modCounts["HD"]++;
                else if (joined.Contains("FL")) modCounts["FL"]++;
                else if (joined.Contains("EZ") || joined.Contains("HT")) modCounts["EZ / HT"]++;
                else modCounts["Other"]++;
            }

            data.ModPlayCounts = modCounts;

            // 5. Star Rating Spread
            var starBuckets = new Dictionary<string, int>
            {
                { "< 4.0★", 0 },
                { "4.0 - 4.9★", 0 },
                { "5.0 - 5.9★", 0 },
                { "6.0 - 6.9★", 0 },
                { "7.0 - 7.9★", 0 },
                { "8.0★+", 0 }
            };

            foreach (var play in allPlays)
            {
                double sr = play.StarRating;
                if (sr < 4.0) starBuckets["< 4.0★"]++;
                else if (sr < 5.0) starBuckets["4.0 - 4.9★"]++;
                else if (sr < 6.0) starBuckets["5.0 - 5.9★"]++;
                else if (sr < 7.0) starBuckets["6.0 - 6.9★"]++;
                else if (sr < 8.0) starBuckets["7.0 - 7.9★"]++;
                else starBuckets["8.0★+"]++;
            }

            data.StarRatingBuckets = starBuckets;

            // 6. Top Beatmaps
            var mapGroups = allPlays
                .GroupBy(p => $"{p.BeatmapArtist} - {p.BeatmapTitle} [{p.DifficultyName}]")
                .Select(g =>
                {
                    var first = g.First();
                    return new TopBeatmapStat
                    {
                        Title = first.BeatmapTitle,
                        Artist = first.BeatmapArtist,
                        Difficulty = first.DifficultyName,
                        Mapper = first.BeatmapMapper,
                        PlayCount = g.Count(),
                        StarRating = first.StarRating,
                        BestAccuracy = g.Max(p => p.Accuracy),
                        BestPp = g.Where(p => p.PerformancePoints.HasValue).Max(p => p.PerformancePoints),
                        MaxCombo = g.Max(p => p.MaxCombo),
                        BestGrade = g.OrderBy(p => p.Rank).First().Rank,
                        OnlineBeatmapID = first.OnlineBeatmapID
                    };
                })
                .OrderByDescending(b => b.PlayCount)
                .Take(8)
                .ToList();

            data.TopBeatmaps = mapGroups;

            // 7. Top Mappers
            var mapperGroups = allPlays
                .Where(p => !string.IsNullOrWhiteSpace(p.BeatmapMapper))
                .GroupBy(p => p.BeatmapMapper)
                .Select(g => new TopMapperStat
                {
                    MapperName = g.Key,
                    PlayCount = g.Count(),
                    AverageAccuracy = g.Average(p => p.Accuracy),
                    MapsCount = g.Select(p => $"{p.BeatmapTitle}_{p.DifficultyName}").Distinct().Count(),
                    TopPp = g.Where(p => p.PerformancePoints.HasValue).Select(p => p.PerformancePoints!.Value).DefaultIfEmpty(0).Max()
                })
                .OrderByDescending(m => m.PlayCount)
                .Take(6)
                .ToList();

            data.TopMappers = mapperGroups;

            // 8. PP Growth Timeline
            var datePpList = new List<(DateTime Date, double CumulativePp, double DayAccuracy)>();
            var dayPlays = allPlays.GroupBy(p => p.Timestamp.ToLocalTime().Date).OrderBy(g => g.Key);
            double runningPp = 0;

            foreach (var group in dayPlays)
            {
                double dayPp = group.Where(p => p.PerformancePoints.HasValue).Sum(p => p.PerformancePoints!.Value);
                runningPp += dayPp;
                datePpList.Add((group.Key, runningPp, group.Average(p => p.Accuracy)));
            }

            data.PpTimeline = datePpList;

            return data;
        }
    }
}
