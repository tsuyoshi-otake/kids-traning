namespace KidsTraining.App.Domain.Learning;

internal enum LearningOutcome
{
    IndependentCorrect,
    AssistedCorrect,
    Revealed,
    Incorrect
}

internal sealed record SkillEvidence(
    int Attempts = 0,
    int IndependentCorrect = 0,
    int AssistedCorrect = 0,
    int Errors = 0,
    double Confidence = 0.05,
    int ReviewStep = 0,
    DateTimeOffset? LastAttemptAt = null,
    DateTimeOffset? NextReviewAt = null,
    DateTimeOffset? MasteredAt = null)
{
    public const int RequiredIndependentCorrect = 8;
    public const int RequiredAttempts = 10;
    public const double RequiredIndependentAccuracy = 0.8;
    public const double RequiredConfidence = 0.8;

    public bool IsAchievement => MasteredAt.HasValue;

    public double IndependentAccuracy => Attempts == 0 ? 0 : (double)IndependentCorrect / Attempts;

    public bool IsReady(DateTimeOffset now) =>
        IndependentCorrect >= RequiredIndependentCorrect &&
        Attempts >= RequiredAttempts &&
        IndependentAccuracy >= RequiredIndependentAccuracy &&
        Confidence >= RequiredConfidence &&
        (!NextReviewAt.HasValue || NextReviewAt.Value > now);

    public bool IsDue(DateTimeOffset now) => NextReviewAt.HasValue && NextReviewAt.Value <= now;

    public SkillEvidence Record(LearningOutcome outcome, DateTimeOffset answeredAt)
    {
        var independent = IndependentCorrect;
        var assisted = AssistedCorrect;
        var errors = Errors;
        var confidence = Confidence;
        var reviewStep = ReviewStep;
        DateTimeOffset nextReview;

        switch (outcome)
        {
            case LearningOutcome.IndependentCorrect:
                independent++;
                confidence = Math.Clamp(confidence + 0.12, 0.05, 0.99);
                if (!NextReviewAt.HasValue || NextReviewAt.Value <= answeredAt)
                {
                    nextReview = ReviewSchedule.NextReview(answeredAt, reviewStep);
                    reviewStep = Math.Min(reviewStep + 1, ReviewSchedule.MaximumStep);
                }
                else
                {
                    nextReview = NextReviewAt.Value;
                }
                break;
            case LearningOutcome.AssistedCorrect:
                assisted++;
                confidence = Math.Clamp(confidence - 0.03, 0.05, 0.99);
                nextReview = answeredAt;
                reviewStep = 0;
                break;
            case LearningOutcome.Revealed:
                assisted++;
                errors++;
                confidence = Math.Clamp(confidence - 0.08, 0.05, 0.99);
                nextReview = answeredAt;
                reviewStep = 0;
                break;
            case LearningOutcome.Incorrect:
                errors++;
                confidence = Math.Clamp(confidence - 0.10, 0.05, 0.99);
                nextReview = answeredAt;
                reviewStep = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null);
        }

        var updated = this with
        {
            Attempts = Attempts + 1,
            IndependentCorrect = independent,
            AssistedCorrect = assisted,
            Errors = errors,
            Confidence = confidence,
            ReviewStep = reviewStep,
            LastAttemptAt = answeredAt,
            NextReviewAt = nextReview
        };

        return updated.IsReady(answeredAt) && !updated.MasteredAt.HasValue
            ? updated with { MasteredAt = answeredAt }
            : updated;
    }
}
