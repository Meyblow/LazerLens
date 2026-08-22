using System.ComponentModel;

namespace LazerLens.Models
{
    public enum DefaultSortMode
    {
        [Description("Time (Recent first)")]
        TimeDesc,

        [Description("Performance Points (PP)")]
        PpDesc,

        [Description("Accuracy (%)")]
        AccDesc,

        [Description("Score")]
        ScoreDesc
    }

    public enum PpDisplayMode
    {
        [Description("Both (+PP & Total PP)")]
        Both,

        [Description("Profile gain only (+PP)")]
        ProfileGainOnly,

        [Description("Beatmap score PP only")]
        ScorePpOnly
    }

    public enum AccuracyCalculationMode
    {
        [Description("Weighted by Hit Objects")]
        ObjectWeighted,

        [Description("Simple Average")]
        SimpleAverage
    }

    public enum SessionSplitThreshold
    {
        [Description("By Day (Midnight)")]
        Midnight,

        [Description("2 Hours of Inactivity")]
        TwoHours,

        [Description("4 Hours of Inactivity")]
        FourHours,

        [Description("Every Game Launch")]
        GameRestart
    }

    public enum AfkPauseTimeout
    {
        [Description("Disabled")]
        Disabled,

        [Description("5 Minutes")]
        FiveMinutes,

        [Description("10 Minutes")]
        TenMinutes,

        [Description("15 Minutes")]
        FifteenMinutes
    }

    public enum ArchiveRetentionLimit
    {
        [Description("Unlimited")]
        Unlimited,

        [Description("30 Days")]
        ThirtyDays,

        [Description("90 Days")]
        NinetyDays,

        [Description("100 Sessions")]
        OneHundredSessions
    }

    public enum PlayNotificationFilter
    {
        [Description("All Plays")]
        All,

        [Description("Passed Plays Only")]
        PassedOnly,

        [Description("Session Bests Only")]
        SessionBestsOnly,

        [Description("Disabled")]
        Disabled
    }

    public enum MilestoneNotificationMode
    {
        [Description("Disabled")]
        Disabled,

        [Description("Every 50 Plays")]
        FiftyPlays,

        [Description("Every 100 Plays")]
        HundredPlays,

        [Description("On +50 PP Gain")]
        FiftyPpGain
    }

    public enum ToolbarBadgeMode
    {
        [Description("No Badge")]
        None,

        [Description("Play Count")]
        PlayCount,

        [Description("Session PP Gain (+PP)")]
        PpGain,

        [Description("Average Accuracy (%)")]
        Accuracy
    }

    public enum SearchBarPosition
    {
        [Description("Right")]
        Right,

        [Description("Centre")]
        Centre
    }
}
