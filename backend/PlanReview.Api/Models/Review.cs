using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// One developer's annual plan &amp; review for a given cycle. Holds the SMART goals,
/// self skill ratings, the developer-selected peer, and the admin-assigned reviewers.
/// </summary>
public class Review
{
    public int Id { get; set; }

    public int ReviewCycleId { get; set; }
    public ReviewCycle? ReviewCycle { get; set; }

    // The developer this review belongs to.
    public int DeveloperId { get; set; }
    public User? Developer { get; set; }

    // Snapshot of the developer's function/role at release time (they can change later).
    public int? FunctionId { get; set; }
    public Function? Function { get; set; }
    public int? RoleId { get; set; }
    public Role? Role { get; set; }

    // Peer selected by the developer (requirement 10).
    public int? SelectedPeerId { get; set; }
    public User? SelectedPeer { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Draft;

    public DateTime? SubmittedAt { get; set; }

    [MaxLength(2000)]
    public string? SelfSummary { get; set; }

    // Half-yearly (mid-year) self update.
    [MaxLength(2000)]
    public string? MidYearReflection { get; set; }
    public DateTime? MidYearUpdatedAt { get; set; }

    public List<Goal> Goals { get; set; } = new();
    public List<Achievement> Achievements { get; set; } = new();
    public List<RndImprovement> RndImprovements { get; set; } = new();
    public List<FutureSkill> FutureSkills { get; set; } = new();
    public List<SkillRating> SkillRatings { get; set; } = new();
    public List<ReviewReviewer> Reviewers { get; set; } = new();
    public List<ReviewerAssessment> Assessments { get; set; } = new();
}
