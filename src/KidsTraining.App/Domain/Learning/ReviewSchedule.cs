namespace KidsTraining.App.Domain.Learning;

internal static class ReviewSchedule
{
    private static readonly TimeSpan[] Intervals =
    [
        TimeSpan.FromDays(1),
        TimeSpan.FromDays(3),
        TimeSpan.FromDays(7),
        TimeSpan.FromDays(21)
    ];

    // Retention is spaced by sessions, not by the clock. A session hands a unit at most one
    // retention review, so the three confirmations already cost three separate sessions, and that
    // boundary is the whole of the spacing: the confirmation gaps are zero, so a child never waits
    // for a star, they practise for it. Day- and hour-long gaps were both tried and both put the
    // star out of reach of a child who practises once in the evening. The last entry is different
    // in kind -- it is the maintenance review that follows mastery, so it stays far out.
    private static readonly TimeSpan[] RetentionIntervals =
    [
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.Zero,
        TimeSpan.FromDays(21)
    ];

    public static int MaximumStep => Intervals.Length - 1;

    public static int MaximumRetentionStep => RetentionIntervals.Length - 1;

    public static TimeSpan IntervalAt(int step) => Intervals[Math.Clamp(step, 0, MaximumStep)];

    public static TimeSpan RetentionIntervalAt(int step) =>
        RetentionIntervals[Math.Clamp(step, 0, MaximumRetentionStep)];

    public static DateTimeOffset NextReview(DateTimeOffset answeredAt, int completedStep) =>
        answeredAt + IntervalAt(completedStep);

    public static DateTimeOffset NextRetentionReview(DateTimeOffset answeredAt, int completedStep) =>
        answeredAt + RetentionIntervalAt(completedStep);
}
