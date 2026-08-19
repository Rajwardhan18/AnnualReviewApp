using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// An assigned reviewer's (manager or peer) assessment of a developer's review:
/// an overall rating, narrative feedback, and per-skill ratings.
/// </summary>
public class ReviewerAssessment
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review? Review { get; set; }

    public int ReviewerId { get; set; }
    public User? Reviewer { get; set; }

    public ReviewerType ReviewerType { get; set; }

    [Range(1, 10)]
    public int OverallRating { get; set; }

    [MaxLength(2000)]
    public string? Strengths { get; set; }

    [MaxLength(2000)]
    public string? Improvements { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public List<ReviewerSkillRating> SkillRatings { get; set; } = new();
}
