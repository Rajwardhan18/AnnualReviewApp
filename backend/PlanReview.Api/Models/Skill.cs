using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// Master list of skills. Skills are mapped to Roles via RoleSkill.
/// </summary>
public class Skill
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Category { get; set; }

    public List<RoleSkill> RoleSkills { get; set; } = new();
}
