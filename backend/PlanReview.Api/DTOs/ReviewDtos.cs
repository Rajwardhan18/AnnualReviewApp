using System.ComponentModel.DataAnnotations;
using PlanReview.Api.Models;

namespace PlanReview.Api.DTOs;

// ---- Goal ----
public record GoalDto(
    int Id,
    GoalType GoalType,
    string Title,
    string Specific,
    string Measurable,
    string Achievable,
    string Relevant,
    string TimeBound,
    int? CompanyTraitId,
    string? CompanyTraitName,
    GoalStatus Status,
    int CompletionPercentage,
    string? StatusComment,
    DateTime? StatusDate,
    string? Target);

// Draft-friendly: only the goal type is required so partial goals can be saved as a draft.
// Professional goals use SMART + trait; Personal goals are simple (Title + Target).
public record GoalInput(
    [Required] GoalType GoalType,
    string? Title,
    string? Specific,
    string? Measurable,
    string? Achievable,
    string? Relevant,
    string? TimeBound,
    int? CompanyTraitId,
    GoalStatus? Status,
    int? CompletionPercentage,
    string? StatusComment,
    DateTime? StatusDate,
    string? Target);

// ---- Goal progress (updatable any time by the developer) ----
public record GoalProgressInput(
    [Required] int GoalId,
    GoalStatus Status,
    [Range(0, 100)] int CompletionPercentage,
    string? StatusComment,
    DateTime? StatusDate);
public record SaveProgressRequest(List<GoalProgressInput> Goals, string? MidYearReflection, string? FinalReflection);

// ---- Previous-year achievement (project delivered last year) ----
public record AchievementDto(
    int Id, string ProjectName, string ClientName, string WorkDescription,
    int? Manager1Rating, int? Manager2Rating, int? CompanyTraitId, string? CompanyTraitName);
// Developers set only the project details — manager ratings are set by managers.
public record AchievementInput(
    string? ProjectName, string? ClientName, string? WorkDescription, int? CompanyTraitId);

// A manager rating the developer's previous-year achievements.
public record AchievementRatingInput([Required] int AchievementId, [Range(1, 10)] int Rating);
public record SaveAchievementRatingsRequest(List<AchievementRatingInput> Ratings);

// ---- R&D improvements & future skills (skill-assessment section) ----
public record RndImprovementDto(int Id, string Description);
public record RndImprovementInput(string? Description);
public record FutureSkillDto(int Id, string Name);
public record FutureSkillInput(string? Name);

// ---- Skill self-rating ----
public record SkillRatingDto(int SkillId, string SkillName, int SelfRating, string? Comments);
public record SkillRatingInput([Required] int SkillId, [Range(1, 10)] int SelfRating, string? Comments);

// ---- Save the plan (draft) ----
public record SavePlanRequest(
    int? SelectedPeerId,
    string? SelfSummary,
    List<GoalInput> Goals,
    List<SkillRatingInput> SkillRatings,
    List<AchievementInput>? Achievements,
    List<RndImprovementInput>? RndImprovements,
    List<FutureSkillInput>? FutureSkills);

// ---- Reviewer info ----
public record ReviewerDto(int ReviewerId, string ReviewerName, ReviewerType ReviewerType, bool HasSubmitted);

// ---- Reviewer assessment ----
public record ReviewerSkillRatingInput([Required] int SkillId, [Range(1, 10)] int Rating);
public record ReviewerSkillRatingDto(int SkillId, string SkillName, int Rating);

public record SubmitAssessmentRequest(
    [Range(1, 10)] int OverallRating,
    string? Strengths,
    string? Improvements,
    List<ReviewerSkillRatingInput> SkillRatings);

public record AssessmentDto(
    int Id,
    int ReviewerId,
    string ReviewerName,
    ReviewerType ReviewerType,
    int OverallRating,
    string? Strengths,
    string? Improvements,
    DateTime? SubmittedAt,
    List<ReviewerSkillRatingDto> SkillRatings);

// ---- Admin assignment ----
public record AssignReviewersRequest(
    [Required] List<int> ManagerIds,   // exactly 2
    [Required] int PeerId);

// ---- Review read models ----
public record ReviewSummaryDto(
    int Id,
    int CycleId,
    string CycleName,
    int DeveloperId,
    string DeveloperName,
    string? FunctionName,
    string? RoleName,
    ReviewStatus Status,
    DateTime? SubmittedAt,
    bool HalfYearlyReleased,
    bool MidYearSubmitted,
    // For the "assigned to me" list: has the current reviewer submitted their assessment?
    bool? MyAssessmentSubmitted);

public record ReviewDetailDto(
    int Id,
    int CycleId,
    string CycleName,
    int DeveloperId,
    string DeveloperName,
    int? FunctionId,
    string? FunctionName,
    int? RoleId,
    string? RoleName,
    ReviewStatus Status,
    DateTime? SubmittedAt,
    int? SelectedPeerId,
    string? SelectedPeerName,
    string? SelfSummary,
    string? MidYearReflection,
    DateTime? MidYearSubmittedAt,
    bool HalfYearlyReleased,
    DateTime? HalfYearlyDueDate,
    string? FinalReflection,
    DateTime? FinalReflectionSubmittedAt,
    bool FinalReviewReleased,
    DateTime? FinalReviewDueDate,
    DateTime? DueDate,
    List<GoalDto> Goals,
    List<AchievementDto> Achievements,
    List<RndImprovementDto> RndImprovements,
    List<FutureSkillDto> FutureSkills,
    List<SkillRatingDto> SkillRatings,
    List<SkillDto> RoleSkills,
    List<ReviewerDto> Reviewers,
    List<AssessmentDto> Assessments,
    /// <summary>Which manager slot the caller occupies (1 or 2), else null.</summary>
    int? MyManagerSlot,
    /// <summary>True when the caller may no longer change their achievement ratings.</summary>
    bool AchievementRatingsLocked);
