using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/cycles")]
[Authorize]
public class CyclesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notify;

    public CyclesController(AppDbContext db, NotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    private static CycleDto ToDto(ReviewCycle c, int reviewCount) => new(
        c.Id, c.Name, c.Year, c.StartDate, c.EndDate, c.IsReleased, c.IsActive, reviewCount,
        c.DueDate, c.HalfYearlyReleased, c.HalfYearlyDueDate);

    [HttpGet]
    public async Task<IEnumerable<CycleDto>> GetAll()
    {
        var rows = await _db.ReviewCycles.OrderByDescending(c => c.Year)
            .Select(c => new { Cycle = c, Count = c.Reviews.Count }).ToListAsync();
        return rows.Select(r => ToDto(r.Cycle, r.Count));
    }

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
            DueDate = req.DueDate is null ? null : DateTime.SpecifyKind(req.DueDate.Value, DateTimeKind.Utc),
            IsActive = true,
            IsReleased = false
        };
        _db.ReviewCycles.Add(cycle);
        await _db.SaveChangesAsync();
        return ToDto(cycle, 0);
    }

    /// <summary>
    /// Release a cycle to all developers: creates a Draft Review for each developer who
    /// doesn't have one, and notifies every developer (with target dates + reminders).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/release")]
    public async Task<ActionResult> Release(int id)
    {
        var cycle = await _db.ReviewCycles.FindAsync(id);
        if (cycle is null) return NotFound();

        var developers = await _db.Users.Where(u => u.UserType == UserType.Developer).ToListAsync();
        var existing = (await _db.Reviews.Where(r => r.ReviewCycleId == id)
            .Select(r => r.DeveloperId).ToListAsync()).ToHashSet();

        var created = 0;
        foreach (var dev in developers)
        {
            if (!existing.Contains(dev.Id))
            {
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
            // Requirement 3: notify every developer that the plan is released.
            await _notify.PlanReleasedAsync(dev, cycle);
        }

        cycle.IsReleased = true;
        await _db.SaveChangesAsync();

        return Ok(new { released = true, reviewsCreated = created, totalDevelopers = developers.Count, notified = developers.Count });
    }

    /// <summary>
    /// Release the half-yearly (mid-year) checkpoint: developers update goal progress and a
    /// mid-year reflection. Manager/peer reviews stay at year-end. Every developer is notified.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/release-halfyearly")]
    public async Task<ActionResult> ReleaseHalfYearly(int id, ReleaseHalfYearlyRequest req)
    {
        var cycle = await _db.ReviewCycles.FindAsync(id);
        if (cycle is null) return NotFound();
        if (!cycle.IsReleased)
            return BadRequest(new { message = "Release the annual plan before the half-yearly review." });

        cycle.HalfYearlyReleased = true;
        cycle.HalfYearlyReleasedAt = DateTime.UtcNow;
        cycle.HalfYearlyDueDate = req.HalfYearlyDueDate is null
            ? null : DateTime.SpecifyKind(req.HalfYearlyDueDate.Value, DateTimeKind.Utc);

        var developers = await _db.Users.Where(u => u.UserType == UserType.Developer).ToListAsync();
        foreach (var dev in developers)
            await _notify.HalfYearlyReleasedAsync(dev, cycle);

        await _db.SaveChangesAsync();
        return Ok(new { halfYearlyReleased = true, notified = developers.Count });
    }
}
