using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Tests;

/// <summary>
/// Covers the weighted-final and normal-curve maths. These decide every developer's
/// published rating, so the edge cases (missing components, a one-person cohort) matter
/// as much as the happy path.
/// </summary>
public class RatingCalculatorTests
{
    /// <summary>A review with two managers in their proper weight slots, plus a peer.</summary>
    private static Review BuildReview(
        double[]? selfRatings = null,
        double? peer = null,
        double? manager1 = null,
        double? manager2 = null)
    {
        var review = new Review { Id = 1 };

        foreach (var r in selfRatings ?? [])
            review.SkillRatings.Add(new SkillRating { SelfRating = (int)r });

        void AddReviewer(int id, ReviewerType type, double weight, double? rating)
        {
            review.Reviewers.Add(new ReviewReviewer { ReviewerId = id, ReviewerType = type, Weight = weight });
            if (rating is not null)
                review.Assessments.Add(new ReviewerAssessment
                {
                    ReviewerId = id,
                    ReviewerType = type,
                    OverallRating = (int)rating.Value,
                    SubmittedAt = DateTime.UtcNow
                });
        }

        if (peer is not null) AddReviewer(10, ReviewerType.Peer, ReviewRules.PeerWeight, peer);
        if (manager1 is not null) AddReviewer(11, ReviewerType.Manager, ReviewRules.Manager1Weight, manager1);
        if (manager2 is not null) AddReviewer(12, ReviewerType.Manager, ReviewRules.Manager2Weight, manager2);

        return review;
    }

    [Fact]
    public void Compute_WeightsAllFourComponents()
    {
        var review = BuildReview(selfRatings: [8, 6], peer: 7, manager1: 9, manager2: 5);

        var result = RatingCalculator.Compute(review);

        Assert.Equal(7, result.Self);          // average of 8 and 6
        Assert.Equal(7, result.Peer);
        Assert.Equal(9, result.Manager1);
        Assert.Equal(5, result.Manager2);
        // 0.10*7 + 0.20*7 + 0.30*9 + 0.40*5 = 0.7 + 1.4 + 2.7 + 2.0 = 6.8
        Assert.Equal(6.8, result.WeightedFinal);
    }

    [Fact]
    public void Compute_AttributesManagerSlotsByWeightNotInsertionOrder()
    {
        var review = BuildReview(manager1: 4, manager2: 10);

        // Shuffle the collections so nothing can depend on the order rows come back in.
        review.Reviewers.Reverse();
        review.Assessments.Reverse();

        var result = RatingCalculator.Compute(review);

        Assert.Equal(4, result.Manager1);
        Assert.Equal(10, result.Manager2);
    }

    [Fact]
    public void Compute_RenormalisesWhenComponentsAreMissing()
    {
        // Only the peer and Manager 2 have reported: weights 0.20 and 0.40 re-spread to sum to 1.
        var result = RatingCalculator.Compute(BuildReview(peer: 6, manager2: 9));

        Assert.Null(result.Self);
        Assert.Null(result.Manager1);
        // (0.20*6 + 0.40*9) / 0.60 = 4.8 / 0.6 = 8
        Assert.Equal(8, result.WeightedFinal);
    }

    [Fact]
    public void Compute_IgnoresAssessmentsThatWereNeverSubmitted()
    {
        var review = BuildReview(selfRatings: [5], peer: 10);
        // An assessment still in progress must not count toward the final.
        review.Assessments.Single(a => a.ReviewerType == ReviewerType.Peer).SubmittedAt = null;

        var result = RatingCalculator.Compute(review);

        Assert.Null(result.Peer);
        Assert.Equal(5, result.WeightedFinal); // self is the only component left
    }

    [Fact]
    public void Compute_ReturnsNullFinalWhenNothingHasBeenRated()
    {
        var result = RatingCalculator.Compute(BuildReview());

        Assert.Null(result.Self);
        Assert.Null(result.WeightedFinal);
    }

    [Fact]
    public void MeanStd_OfSingleDeveloperCohortHasZeroDeviation()
    {
        var (mean, std) = RatingCalculator.MeanStd([7.5]);

        Assert.Equal(7.5, mean);
        Assert.Equal(0, std);
    }

    [Fact]
    public void MeanStd_OfEmptyCohortIsZeroed()
    {
        var (mean, std) = RatingCalculator.MeanStd([]);

        Assert.Equal(0, mean);
        Assert.Equal(0, std);
    }

    [Fact]
    public void MeanStd_ComputesPopulationDeviation()
    {
        // Population (not sample) variance of 2,4,4,4,5,5,7,9 is 4 -> std 2.
        var (mean, std) = RatingCalculator.MeanStd([2, 4, 4, 4, 5, 5, 7, 9]);

        Assert.Equal(5, mean);
        Assert.Equal(2, std, 10);
    }

    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(1.0, 0.8413)]
    [InlineData(-1.0, 0.1587)]
    [InlineData(1.96, 0.9750)]
    public void NormalCdf_MatchesTheStandardNormalTable(double z, double expected)
    {
        Assert.Equal(expected, RatingCalculator.NormalCdf(z), 4);
    }

    [Fact]
    public void NormalCdf_IsSymmetricAboutZero()
    {
        Assert.Equal(1.0, RatingCalculator.NormalCdf(1.2) + RatingCalculator.NormalCdf(-1.2), 10);
    }

    [Theory]
    [InlineData(2.0, "Outstanding")]
    [InlineData(1.5, "Outstanding")]   // boundary is inclusive
    [InlineData(1.49, "Exceeds")]
    [InlineData(0.5, "Exceeds")]
    [InlineData(0.49, "Meets")]
    [InlineData(0.0, "Meets")]
    [InlineData(-0.5, "Meets")]
    [InlineData(-0.51, "Below")]
    [InlineData(-1.5, "Below")]
    [InlineData(-1.51, "Needs Improvement")]
    public void Band_SplitsAtHalfAndOneAndAHalfSigma(double z, string expected)
    {
        Assert.Equal(expected, RatingCalculator.Band(z));
    }
}
