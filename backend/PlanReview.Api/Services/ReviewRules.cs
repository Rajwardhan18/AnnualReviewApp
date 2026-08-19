namespace PlanReview.Api.Services;

/// <summary>Business rules for review submission (requirements 7, 10, 11).</summary>
public static class ReviewRules
{
    public const int MinProfessionalGoals = 5;
    public const int MinPersonalGoals = 2;
    public const int RequiredManagers = 2;
    public const int RequiredPeers = 1;

    // Weights for the normalized final rating (must sum to 1.0).
    public const double SelfWeight = 0.10;
    public const double PeerWeight = 0.20;
    public const double Manager1Weight = 0.30;
    public const double Manager2Weight = 0.40;
}
