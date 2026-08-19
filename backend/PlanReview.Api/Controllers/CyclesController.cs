using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/cycles")]
[Authorize]
public class CyclesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CyclesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<CycleDto>> GetAll() =>
        await _db.ReviewCycles.OrderByDescending(c => c.Year)
            .Select(c => new CycleDto(c.Id, c.Name, c.Year, c.StartDate, c.EndDate,
                c.IsReleased, c.IsActive, c.Reviews.Count))
            .ToListAsync();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CycleDto>> Create(CreateCycleRequest req)
    {
        var cycle = new ReviewCycle
        {
            Name = req.Name.Trim(),
            Year = req.Year,
            StartDate = DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(req.EndDate, DateTimeKind.Utc),
            IsActive = true,
            IsReleased = false
        };
        _db.ReviewCycles.Add(cycle);
        await _db.SaveChangesAsync();
        return new CycleDto(cycle.Id, cycle.Name, cycle.Year, cycle.StartDate, cycle.EndDate,
            cycle.IsReleased, cycle.IsActive, 0);
    }

    /// <summary>
    /// Release a cycle to all developers: creates a Draft Review for each developer
    /// who doesn't already have one for this cycle (requirement 5).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/release")]
    public async Task<ActionResult> Release(int id)
    {
        var cycle = await _db.ReviewCycles.FindAsync(id);
        if (cycle is null) return NotFound();

        var developers = await _db.Users
            .Where(u => u.UserType == UserType.Developer)
            .ToListAsync();

        var existing = await _db.Reviews
            .Where(r => r.ReviewCycleId == id)
            .Select(r => r.DeveloperId)
            .ToListAsync();
        var existingSet = existing.ToHashSet();

        var created = 0;
        foreach (var dev in developers)
        {
            if (existingSet.Contains(dev.Id)) continue;
            _db.Reviews.Add(new Review
            {
                ReviewCycleId = id,
                DeveloperId = dev.Id,
                FunctionId = dev.FunctionId,
                RoleId = dev.RoleId,
                Status = ReviewStatus.Draft
            });
            created++;
        }

        cycle.IsReleased = true;
        await _db.SaveChangesAsync();

        return Ok(new { released = true, reviewsCreated = created, totalDevelopers = developers.Count });
    }
}
