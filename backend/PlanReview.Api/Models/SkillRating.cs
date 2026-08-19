using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// The developer's self-rating against a single skill identified for their role.
/// </summary>
public class SkillRating
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review? Review { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }

    /// <summary>Self rating 1-10.</summary>
    [Range(1, 10)]
    public int SelfRating { get; set; }

    [MaxLength(1000)]
    public string? Comments { get; set; }
}
