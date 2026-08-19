using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>A key R&amp;D / research contribution, captured in the skill-assessment section.</summary>
public class RndImprovement
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review? Review { get; set; }

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}
