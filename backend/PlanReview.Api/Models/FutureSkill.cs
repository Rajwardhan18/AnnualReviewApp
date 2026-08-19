using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>A skill the developer plans to acquire, captured in the skill-assessment section.</summary>
public class FutureSkill
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review? Review { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
