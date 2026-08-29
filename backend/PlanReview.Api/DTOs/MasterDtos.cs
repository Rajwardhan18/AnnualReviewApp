using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.DTOs;

public record FunctionDto(int Id, string Name, string? Description);
public record CreateFunctionRequest([Required] string Name, string? Description);

public record RoleDto(int Id, string Name, int FunctionId, string FunctionName);
public record CreateRoleRequest([Required] string Name, [Required] int FunctionId);

public record SkillDto(int Id, string Name, string? Category);
public record CreateSkillRequest([Required] string Name, string? Category);

public record CompanyTraitDto(int Id, string Name, string? Description);
public record CreateTraitRequest([Required] string Name, string? Description);

public record MapRoleSkillsRequest([Required] int RoleId, [Required] List<int> SkillIds);

public record CycleDto(
    int Id, string Name, int Year, DateTime StartDate, DateTime EndDate,
    bool IsReleased, bool IsActive, int ReviewCount,
    DateTime? DueDate, bool HalfYearlyReleased, DateTime? HalfYearlyDueDate,
    bool FinalReviewReleased, DateTime? FinalReviewDueDate,
    bool RatingsReleased, bool Ended);

public record CreateCycleRequest(
    [Required] string Name, [Required] int Year, DateTime StartDate, DateTime EndDate, DateTime? DueDate);

public record ReleaseHalfYearlyRequest(DateTime? HalfYearlyDueDate);
public record ReleaseFinalReviewRequest(DateTime? FinalReviewDueDate);
