using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Tests;

/// <summary>
/// Achievement ratings follow the same submit-and-freeze rule as the plan, the mid-year
/// checkpoint and the reviewer assessments — and must never move after the developer has
/// been shown their released ratings.
/// </summary>
public class AchievementRatingLockTests
{
    private const int Manager1Id = 11;
    private const int Manager2Id = 12;

    private static Review BuildReview(
        bool manager1Submitted = false,
        bool manager2Submitted = false,
        bool ratingsReleased = false,
        bool ended = false)
    {
        var review = new Review
        {
            Id = 1,
            ReviewCycle = new ReviewCycle { Id = 1, Name = "FY2026", RatingsReleased = ratingsReleased, Ended = ended }
        };

        review.Reviewers.Add(new ReviewReviewer
        { ReviewerId = Manager1Id, ReviewerType = ReviewerType.Manager, Weight = ReviewRules.Manager1Weight });
        review.Reviewers.Add(new ReviewReviewer
        { ReviewerId = Manager2Id, ReviewerType = ReviewerType.Manager, Weight = ReviewRules.Manager2Weight });

        if (manager1Submitted)
            review.Assessments.Add(new ReviewerAssessment
            { ReviewerId = Manager1Id, ReviewerType = ReviewerType.Manager, SubmittedAt = DateTime.UtcNow });
        if (manager2Submitted)
            review.Assessments.Add(new ReviewerAssessment
            { ReviewerId = Manager2Id, ReviewerType = ReviewerType.Manager, SubmittedAt = DateTime.UtcNow });

        return review;
    }

    [Fact]
    public void Open_WhileTheCycleIsRunningAndNothingIsSubmitted()
    {
        Assert.False(ReviewRules.AchievementRatingsLocked(BuildReview(), Manager1Id));
    }

    [Fact]
    public void Locked_OnceThatManagerHasSubmittedTheirAssessment()
    {
        var review = BuildReview(manager1Submitted: true);

        Assert.True(ReviewRules.AchievementRatingsLocked(review, Manager1Id));
    }

    [Fact]
    public void Open_ForAManagerWhoseColleagueHasSubmittedButWhoHasNot()
    {
        // Manager 2 submitting must not freeze Manager 1 out of their own ratings.
        var review = BuildReview(manager2Submitted: true);

        Assert.False(ReviewRules.AchievementRatingsLocked(review, Manager1Id));
        Assert.True(ReviewRules.AchievementRatingsLocked(review, Manager2Id));
    }

    [Fact]
    public void Locked_ForEveryoneOnceRatingsAreReleasedToTheDeveloper()
    {
        var review = BuildReview(ratingsReleased: true);

        Assert.True(ReviewRules.AchievementRatingsLocked(review, Manager1Id));
        Assert.True(ReviewRules.AchievementRatingsLocked(review, Manager2Id));
    }

    [Fact]
    public void Locked_ForEveryoneOnceTheCycleHasEnded()
    {
        var review = BuildReview(ended: true);

        Assert.True(ReviewRules.AchievementRatingsLocked(review, Manager1Id));
        Assert.True(ReviewRules.AchievementRatingsLocked(review, Manager2Id));
    }

    [Fact]
    public void Open_WhenAnAssessmentExistsButWasNeverSubmitted()
    {
        var review = BuildReview();
        review.Assessments.Add(new ReviewerAssessment
        { ReviewerId = Manager1Id, ReviewerType = ReviewerType.Manager, SubmittedAt = null });

        Assert.False(ReviewRules.AchievementRatingsLocked(review, Manager1Id));
    }
}
