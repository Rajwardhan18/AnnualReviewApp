using PlanReview.Api.Models;

namespace PlanReview.Api.DTOs;

public record MyGoalProgressDto(
    int Id, string Title, GoalType GoalType, GoalStatus Status, int CompletionPercentage, string? Target);

public record MyPerformanceCycleDto(
    int ReviewId,
    int CycleId,
    string CycleName,
    ReviewStatus Status,
    bool RatingsReleased,
    // Ratings (only populated once the admin has released them)
    double? SelfScore,
    double? PeerScore,
    double? Manager1Score,
    double? Manager2Score,
    double? WeightedFinal,
    double? OverallAverage,
    double? Percentile,
    string? Band,
    double? TeamAverage,
    // Self-progress (always available)
    int GoalCount,
    double AvgCompletion,
    int Completed,
    int InProgress,
    int NotStarted,
    int Dropped,
    List<MyGoalProgressDto> Goals);

public record MyPerformanceDto(List<MyPerformanceCycleDto> Cycles);
