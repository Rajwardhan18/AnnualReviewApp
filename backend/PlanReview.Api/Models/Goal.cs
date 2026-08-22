using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// A SMART goal within a review. Professional goals (min 5) and Personal goals (min 2).
/// Every goal is tagged against a CompanyTrait.
/// </summary>
public class Goal
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review? Review { get; set; }

    public GoalType GoalType { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    // SMART template fields. Nullable-friendly so partial drafts can be saved;
    // completeness is enforced on submit.
    [MaxLength(1000)]
    public string Specific { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Measurable { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Achievable { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Relevant { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string TimeBound { get; set; } = string.Empty;

    /// <summary>Simple target for Personal goals (which do not use the SMART template).</summary>
    [MaxLength(1000)]
    public string? Target { get; set; }

    // Requirement 8: each goal tagged against a company trait (required on submit).
    public int? CompanyTraitId { get; set; }
    public CompanyTrait? CompanyTrait { get; set; }

    // Progress tracking (updatable by the developer through the year).
    public GoalStatus Status { get; set; } = GoalStatus.NotStarted;

    [Range(0, 100)]
    public int CompletionPercentage { get; set; }

    [MaxLength(1000)]
    public string? StatusComment { get; set; }

    public DateTime? StatusDate { get; set; }
}
