using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// A "last year" key achievement — a project the developer delivered, with the client,
/// a work description, the rating their manager gave it, and an optional company trait.
/// </summary>
public class Achievement
{
    public int Id { get; set; }

    public int ReviewId { get; set; }
    public Review? Review { get; set; }

    [MaxLength(200)]
    public string ProjectName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ClientName { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string WorkDescription { get; set; } = string.Empty;

    /// <summary>Rating from the first assigned manager (set only by that manager).</summary>
    [Range(1, 10)]
    public int? Manager1Rating { get; set; }

    /// <summary>Rating from the second assigned manager (set only by that manager).</summary>
    [Range(1, 10)]
    public int? Manager2Rating { get; set; }

    // Optional company-trait tag.
    public int? CompanyTraitId { get; set; }
    public CompanyTrait? CompanyTrait { get; set; }
}
