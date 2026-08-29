using PlanReview.Api.Models;

namespace PlanReview.Api.Services;

/// <summary>Business rules for review submission (requirements 7, 10, 11).</summary>
public static class ReviewRules
{
    /// <summary>
    /// Whether the previous-year achievement ratings can still be changed. Rating achievements is
    /// the reviewers' <em>initial</em>-phase task, so the ratings freeze once the half-yearly review
    /// is released (and stay frozen once ratings are released or the cycle ends). The manager id is
    /// no longer part of the rule — achievement ratings freeze for everyone at the same phase.
    /// </summary>
    public static bool AchievementRatingsLocked(Review review, int managerId = 0) =>
        review.ReviewCycle?.HalfYearlyReleased == true
        || review.ReviewCycle?.RatingsReleased == true
        || review.ReviewCycle?.Ended == true;

    public const int MinProfessionalGoals = 5;
    public const int MinPersonalGoals = 2;
    public const int MinAchievements = 5;
    public const int RequiredManagers = 2;
    public const int RequiredPeers = 1;

    // Weights for the normalized final rating (must sum to 1.0).
    public const double SelfWeight = 0.10;
    public const double PeerWeight = 0.20;
    public const double Manager1Weight = 0.30;
    public const double Manager2Weight = 0.40;
}
