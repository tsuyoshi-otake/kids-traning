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
    int RetentionStep = 0,
    DateTimeOffset? LastAttemptAt = null,
    DateTimeOffset? NextReviewAt = null,
    DateTimeOffset? RetentionStartedAt = null,
    DateTimeOffset? MasteredAt = null)
{
    public const int RequiredStageIndependentCorrect = 5;
    public const int RequiredStageAttempts = 6;
    public const int RequiredRetentionConfirmations = 3;

    public bool IsAchievement => MasteredAt.HasValue;

    public bool IsRetentionActive => RetentionStartedAt.HasValue;

    public double IndependentAccuracy => Attempts == 0 ? 0 : (double)IndependentCorrect / Attempts;

    public bool IsQualifiedForRetention =>
        IndependentCorrect >= RequiredStageIndependentCorrect &&
        Attempts >= RequiredStageAttempts &&
        IndependentAccuracy >= (double)RequiredStageIndependentCorrect / RequiredStageAttempts;

    public bool IsReady(DateTimeOffset now) =>
        IsAchievement &&
        RetentionStep >= RequiredRetentionConfirmations &&
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

        return updated;
    }

    public SkillEvidence StartRetention(DateTimeOffset startedAt)
    {
        if (IsRetentionActive || !IsQualifiedForRetention)
        {
            return this;
        }

        return this with
        {
            ReviewStep = 0,
            RetentionStep = 0,
            RetentionStartedAt = startedAt,
            NextReviewAt = ReviewSchedule.NextReview(startedAt, 0)
        };
    }

    public SkillEvidence RecordRetentionReview(LearningOutcome outcome, DateTimeOffset answeredAt)
    {
        if (!IsRetentionActive || !IsDue(answeredAt))
        {
            return this;
        }

        var independent = IndependentCorrect;
        var assisted = AssistedCorrect;
        var errors = Errors;
        var confidence = Confidence;
        var retentionStep = RetentionStep;
        DateTimeOffset nextReview;

        if (outcome == LearningOutcome.IndependentCorrect)
        {
            independent++;
            confidence = Math.Clamp(confidence + 0.12, 0.05, 0.99);
            retentionStep = Math.Min(retentionStep + 1, RequiredRetentionConfirmations);
            nextReview = ReviewSchedule.NextReview(answeredAt, retentionStep);
        }
        else
        {
            if (outcome is LearningOutcome.AssistedCorrect or LearningOutcome.Revealed)
            {
                assisted++;
            }

            if (outcome is LearningOutcome.Revealed or LearningOutcome.Incorrect)
            {
                errors++;
            }

            confidence = Math.Clamp(
                confidence - (outcome == LearningOutcome.AssistedCorrect ? 0.03 : outcome == LearningOutcome.Revealed ? 0.08 : 0.10),
                0.05,
                0.99);
            retentionStep = 0;
            nextReview = ReviewSchedule.NextReview(answeredAt, 0);
        }

        return this with
        {
            Attempts = Attempts + 1,
            IndependentCorrect = independent,
            AssistedCorrect = assisted,
            Errors = errors,
            Confidence = confidence,
            ReviewStep = retentionStep,
            RetentionStep = retentionStep,
            LastAttemptAt = answeredAt,
            NextReviewAt = nextReview,
            MasteredAt = retentionStep >= RequiredRetentionConfirmations
                ? MasteredAt ?? answeredAt
                : MasteredAt
        };
    }
}
