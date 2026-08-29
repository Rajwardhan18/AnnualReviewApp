using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Tests;

/// <summary>
/// Rating the previous-year achievements is the reviewers' initial-phase task, so the ratings
/// stay open through the initial window and freeze once the half-yearly review is released
/// (and remain frozen once ratings are released or the cycle ends).
/// </summary>
public class AchievementRatingLockTests
{
    private const int Manager1Id = 11;

    private static Review BuildReview(
        bool halfYearlyReleased = false,
        bool ratingsReleased = false,
        bool ended = false)
    {
        var review = new Review
        {
            Id = 1,
            ReviewCycle = new ReviewCycle
            {
                Id = 1,
                Name = "FY2026",
                HalfYearlyReleased = halfYearlyReleased,
                RatingsReleased = ratingsReleased,
                Ended = ended
            }
        };

        review.Reviewers.Add(new ReviewReviewer
        { ReviewerId = Manager1Id, ReviewerType = ReviewerType.Manager, Weight = ReviewRules.Manager1Weight });

        return review;
    }

    [Fact]
    public void Open_DuringTheInitialWindow()
    {
        Assert.False(ReviewRules.AchievementRatingsLocked(BuildReview()));
    }

    [Fact]
    public void Locked_OnceTheHalfYearlyReviewIsReleased()
    {
        Assert.True(ReviewRules.AchievementRatingsLocked(BuildReview(halfYearlyReleased: true)));
    }

    [Fact]
    public void Locked_OnceRatingsAreReleasedToTheDeveloper()
    {
        Assert.True(ReviewRules.AchievementRatingsLocked(BuildReview(ratingsReleased: true)));
    }

    [Fact]
    public void Locked_OnceTheCycleHasEnded()
    {
        Assert.True(ReviewRules.AchievementRatingsLocked(BuildReview(ended: true)));
    }

    [Fact]
    public void NotLocked_JustBecauseAManagerSubmittedTheirYearEndAssessment()
    {
        // The achievement wave is independent of the (year-end) assessment wave.
        var review = BuildReview();
        review.Assessments.Add(new ReviewerAssessment
        { ReviewerId = Manager1Id, ReviewerType = ReviewerType.Manager, SubmittedAt = DateTime.UtcNow });

        Assert.False(ReviewRules.AchievementRatingsLocked(review));
    }
}
