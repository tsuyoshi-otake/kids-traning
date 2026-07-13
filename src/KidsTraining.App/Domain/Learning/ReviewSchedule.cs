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

    public static int MaximumStep => Intervals.Length - 1;

    public static TimeSpan IntervalAt(int step) => Intervals[Math.Clamp(step, 0, MaximumStep)];

    public static DateTimeOffset NextReview(DateTimeOffset answeredAt, int completedStep) =>
        answeredAt + IntervalAt(completedStep);
}
