using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// A career role within a Function, e.g. "SDE-1", "SDE-2", "Senior Engineer".
/// Each Role maps to a set of Skills via RoleSkill.
/// </summary>
public class Role
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int FunctionId { get; set; }
    public Function? Function { get; set; }

    public List<RoleSkill> RoleSkills { get; set; } = new();
}
