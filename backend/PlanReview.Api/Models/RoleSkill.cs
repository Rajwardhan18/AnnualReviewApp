namespace PlanReview.Api.Models;

/// <summary>
/// Maps a master Skill to a Role (the "skills master mapped against each role").
/// </summary>
public class RoleSkill
{
    public int Id { get; set; }

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public int SkillId { get; set; }
    public Skill? Skill { get; set; }
}
