using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// An annual plan-and-review cycle, e.g. "FY2026". When released, a Review is
/// created for every developer.
/// </summary>
public class ReviewCycle
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public int Year { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>True once the cycle has been released to developers.</summary>
    public bool IsReleased { get; set; }

    /// <summary>Target date for developers to complete/submit their annual plan.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Half-yearly (mid-year) checkpoint.</summary>
    public bool HalfYearlyReleased { get; set; }
    public DateTime? HalfYearlyReleasedAt { get; set; }
    public DateTime? HalfYearlyDueDate { get; set; }

    /// <summary>Admin has released final ratings to developers (visible on My Performance).</summary>
    public bool RatingsReleased { get; set; }
    public DateTime? RatingsReleasedAt { get; set; }

    /// <summary>Admin has ended the cycle (a separate step from releasing ratings).</summary>
    public bool Ended { get; set; }
    public DateTime? EndedAt { get; set; }

    /// <summary>Only one cycle is typically active at a time.</summary>
    public bool IsActive { get; set; } = true;

    public List<Review> Reviews { get; set; } = new();
}
