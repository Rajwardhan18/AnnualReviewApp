using PlanReview.Api.Models;

namespace PlanReview.Api.DTOs;

/// <summary>Component scores (all 1-10) and the normalized/curve-fitted final for one developer.</summary>
public record DeveloperRatingRow(
    int ReviewId,
    int DeveloperId,
    string DeveloperName,
    string? FunctionName,
    string? RoleName,
    int CycleId,
    string CycleName,
    ReviewStatus Status,
    double? SelfScore,       // weight 10%
    double? PeerScore,       // weight 20%
    double? Manager1Score,   // weight 30%
    double? Manager2Score,   // weight 40%
    double? WeightedFinal,   // normalized weighted average (1-10)
    double? ZScore,          // standard score on the cohort's normal curve
    double? Percentile,      // Φ(z) * 100
    double? CurvedScore,     // final mapped onto the fitted normal curve (1-10)
    string? Band);           // performance band from the curve

/// <summary>Band bucket. LowerZ/UpperZ are null when unbounded (±∞).</summary>
public record BandBucket(string Band, int Count, double? LowerZ, double? UpperZ);

public record CurveStats(
    int Count,
    double Mean,
    double StdDev,
    double Min,
    double Max,
    List<BandBucket> Buckets);

public record RatingWeights(double Self, double Peer, double Manager1, double Manager2);

public record RatingsDashboard(
    int CycleId,
    string CycleName,
    RatingWeights Weights,
    CurveStats Curve,
    List<DeveloperRatingRow> Developers);
