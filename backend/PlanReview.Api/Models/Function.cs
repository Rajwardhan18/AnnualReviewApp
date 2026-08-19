using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// A developer discipline, e.g. "Frontend Developer" or "Backend Developer".
/// A Function owns a set of career Roles (SDE-1, SDE-2, ...).
/// </summary>
public class Function
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<Role> Roles { get; set; } = new();
}
