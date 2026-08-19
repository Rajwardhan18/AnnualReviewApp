using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api")]
public class MasterDataController : ControllerBase
{
    private readonly AppDbContext _db;
    public MasterDataController(AppDbContext db) => _db = db;

    // ---------------- Functions ----------------
    // Anonymous GET so the registration screen can populate function/role pickers.
    [HttpGet("functions")]
    public async Task<IEnumerable<FunctionDto>> GetFunctions() =>
        await _db.Functions.OrderBy(f => f.Name)
            .Select(f => new FunctionDto(f.Id, f.Name, f.Description)).ToListAsync();

    [Authorize(Roles = "Admin")]
    [HttpPost("functions")]
    public async Task<ActionResult<FunctionDto>> CreateFunction(CreateFunctionRequest req)
    {
        var f = new Function { Name = req.Name.Trim(), Description = req.Description };
        _db.Functions.Add(f);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetFunctions), new FunctionDto(f.Id, f.Name, f.Description));
    }

    // ---------------- Roles ----------------
    [HttpGet("roles")]
    public async Task<IEnumerable<RoleDto>> GetRoles([FromQuery] int? functionId)
    {
        var q = _db.Roles.Include(r => r.Function).AsQueryable();
        if (functionId is not null) q = q.Where(r => r.FunctionId == functionId);
        return await q.OrderBy(r => r.Function!.Name).ThenBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name, r.FunctionId, r.Function!.Name)).ToListAsync();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("roles")]
    public async Task<ActionResult<RoleDto>> CreateRole(CreateRoleRequest req)
    {
        var function = await _db.Functions.FindAsync(req.FunctionId);
        if (function is null) return BadRequest(new { message = "Function not found." });

        var r = new Role { Name = req.Name.Trim(), FunctionId = req.FunctionId };
        _db.Roles.Add(r);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRoles), new RoleDto(r.Id, r.Name, r.FunctionId, function.Name));
    }

    // ---------------- Skills ----------------
    [Authorize]
    [HttpGet("skills")]
    public async Task<IEnumerable<SkillDto>> GetSkills() =>
        await _db.Skills.OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new SkillDto(s.Id, s.Name, s.Category)).ToListAsync();

    [Authorize(Roles = "Admin")]
    [HttpPost("skills")]
    public async Task<ActionResult<SkillDto>> CreateSkill(CreateSkillRequest req)
    {
        var s = new Skill { Name = req.Name.Trim(), Category = req.Category };
        _db.Skills.Add(s);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSkills), new SkillDto(s.Id, s.Name, s.Category));
    }

    // Skills mapped to a specific role (the "skills identified for the role").
    [Authorize]
    [HttpGet("roles/{roleId:int}/skills")]
    public async Task<IEnumerable<SkillDto>> GetRoleSkills(int roleId) =>
        await _db.RoleSkills.Where(rs => rs.RoleId == roleId)
            .Include(rs => rs.Skill)
            .OrderBy(rs => rs.Skill!.Category).ThenBy(rs => rs.Skill!.Name)
            .Select(rs => new SkillDto(rs.Skill!.Id, rs.Skill.Name, rs.Skill.Category))
            .ToListAsync();

    [Authorize(Roles = "Admin")]
    [HttpPut("roles/skills")]
    public async Task<IActionResult> MapRoleSkills(MapRoleSkillsRequest req)
    {
        var role = await _db.Roles.FindAsync(req.RoleId);
        if (role is null) return BadRequest(new { message = "Role not found." });

        var existing = _db.RoleSkills.Where(rs => rs.RoleId == req.RoleId);
        _db.RoleSkills.RemoveRange(existing);

        foreach (var skillId in req.SkillIds.Distinct())
            _db.RoleSkills.Add(new RoleSkill { RoleId = req.RoleId, SkillId = skillId });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---------------- Company traits ----------------
    [Authorize]
    [HttpGet("traits")]
    public async Task<IEnumerable<CompanyTraitDto>> GetTraits() =>
        await _db.CompanyTraits.OrderBy(t => t.Name)
            .Select(t => new CompanyTraitDto(t.Id, t.Name, t.Description)).ToListAsync();

    [Authorize(Roles = "Admin")]
    [HttpPost("traits")]
    public async Task<ActionResult<CompanyTraitDto>> CreateTrait(CreateTraitRequest req)
    {
        var t = new CompanyTrait { Name = req.Name.Trim(), Description = req.Description };
        _db.CompanyTraits.Add(t);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTraits), new CompanyTraitDto(t.Id, t.Name, t.Description));
    }
}
