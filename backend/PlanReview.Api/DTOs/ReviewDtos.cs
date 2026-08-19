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
    DateTime? StatusDate);

// Draft-friendly: only the goal type is required so partial goals can be saved as a draft.
// Full completeness (all SMART fields + trait) is enforced on submit.
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
    DateTime? StatusDate);

// ---- Goal progress (updatable any time by the developer) ----
public record GoalProgressInput(
    [Required] int GoalId,
    GoalStatus Status,
    [Range(0, 100)] int CompletionPercentage,
    string? StatusComment,
    DateTime? StatusDate);
public record SaveProgressRequest(List<GoalProgressInput> Goals);

// ---- Last-year key achievement (project delivered last year) ----
public record AchievementDto(
    int Id, string ProjectName, string ClientName, string WorkDescription,
    int? ManagerRating, int? CompanyTraitId, string? CompanyTraitName);
public record AchievementInput(
    string? ProjectName, string? ClientName, string? WorkDescription,
    int? ManagerRating, int? CompanyTraitId);

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
    DateTime? SubmittedAt);

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
    List<GoalDto> Goals,
    List<AchievementDto> Achievements,
    List<RndImprovementDto> RndImprovements,
    List<FutureSkillDto> FutureSkills,
    List<SkillRatingDto> SkillRatings,
    List<SkillDto> RoleSkills,
    List<ReviewerDto> Reviewers,
    List<AssessmentDto> Assessments);
