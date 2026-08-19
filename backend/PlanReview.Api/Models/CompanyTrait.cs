using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// Master list of company traits/values, e.g. "Leadership", "Ownership", "Integrity".
/// Every goal is tagged against one of these.
/// </summary>
public class CompanyTrait
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Description { get; set; }
}
