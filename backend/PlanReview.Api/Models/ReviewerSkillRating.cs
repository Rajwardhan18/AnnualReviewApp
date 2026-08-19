using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// A reviewer's rating of the developer against a single role skill,
/// captured as part of a ReviewerAssessment.
/// </summary>
public class ReviewerSkillRating
{
    public int Id { get; set; }

    public int ReviewerAssessmentId { get; set; }
    public ReviewerAssessment? ReviewerAssessment { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }

    [Range(1, 10)]
    public int Rating { get; set; }
}
