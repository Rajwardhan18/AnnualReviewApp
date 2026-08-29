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
        c.DueDate, c.HalfYearlyReleased, c.HalfYearlyDueDate,
        c.FinalReviewReleased, c.FinalReviewDueDate, c.RatingsReleased, c.Ended);

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

        var developers = await _db.Users.Where(u => u.UserType == UserType.Developer && u.IsActive).ToListAsync();
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

        var developers = await _db.Users.Where(u => u.UserType == UserType.Developer && u.IsActive).ToListAsync();
        foreach (var dev in developers)
            await _notify.HalfYearlyReleasedAsync(dev, cycle);

        await _db.SaveChangesAsync();
        return Ok(new { halfYearlyReleased = true, notified = developers.Count });
    }

    /// <summary>
    /// Release the year-end (final) review window: managers and peers complete their year-end
    /// assessments. This must be released before the cycle can be closed. Developers are notified.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/release-finalreview")]
    public async Task<ActionResult> ReleaseFinalReview(int id, ReleaseFinalReviewRequest req)
    {
        var cycle = await _db.ReviewCycles.FindAsync(id);
        if (cycle is null) return NotFound();
        if (!cycle.IsReleased)
            return BadRequest(new { message = "Release the annual plan before the year-end review." });
        if (!cycle.HalfYearlyReleased)
            return BadRequest(new { message = "Release the half-yearly review before the year-end review." });
        if (cycle.FinalReviewReleased)
            return BadRequest(new { message = "The year-end review has already been released." });

        cycle.FinalReviewReleased = true;
        cycle.FinalReviewReleasedAt = DateTime.UtcNow;
        cycle.FinalReviewDueDate = req.FinalReviewDueDate is null
            ? null : DateTime.SpecifyKind(req.FinalReviewDueDate.Value, DateTimeKind.Utc);

        var developers = await _db.Users.Where(u => u.UserType == UserType.Developer && u.IsActive).ToListAsync();
        foreach (var dev in developers)
            await _notify.FinalReviewReleasedAsync(dev, cycle);

        await _db.SaveChangesAsync();
        return Ok(new { finalReviewReleased = true, notified = developers.Count });
    }

    /// <summary>
    /// Release final ratings to developers — they become visible on My Performance. Only allowed
    /// once the cycle has been closed (ended).
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/release-ratings")]
    public async Task<ActionResult> ReleaseRatings(int id)
    {
        var cycle = await _db.ReviewCycles.FindAsync(id);
        if (cycle is null) return NotFound();
        if (!cycle.Ended)
            return BadRequest(new { message = "Close the cycle before releasing ratings." });
        if (cycle.RatingsReleased)
            return BadRequest(new { message = "Ratings have already been released." });

        cycle.RatingsReleased = true;
        cycle.RatingsReleasedAt = DateTime.UtcNow;

        var developers = await _db.Users.Where(u => u.UserType == UserType.Developer && u.IsActive).ToListAsync();
        foreach (var dev in developers)
            await _notify.RatingsReleasedAsync(dev, cycle);

        await _db.SaveChangesAsync();
        return Ok(new { ratingsReleased = true, notified = developers.Count });
    }

    /// <summary>
    /// End (close) the cycle. Only allowed once the year-end review has been released, the
    /// half-yearly review has been submitted by everyone, and all manager &amp; peer reviews have
    /// been submitted (all reviews Completed). Closing the cycle is what makes ratings releasable.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/end")]
    public async Task<ActionResult> End(int id)
    {
        var cycle = await _db.ReviewCycles.FindAsync(id);
        if (cycle is null) return NotFound();
        if (!cycle.IsReleased)
            return BadRequest(new { message = "Release the annual plan before ending the cycle." });
        if (cycle.Ended)
            return BadRequest(new { message = "This cycle has already ended." });

        var reviews = await _db.Reviews.Where(r => r.ReviewCycleId == id)
            .Include(r => r.Reviewers).ToListAsync();
        var active = reviews.Where(r => r.Status != ReviewStatus.Draft).ToList();

        var errors = new List<string>();
        if (!cycle.HalfYearlyReleased)
            errors.Add("The half-yearly review has not been released.");
        else
        {
            var midYearPending = active.Count(r => r.MidYearSubmittedAt is null);
            if (midYearPending > 0)
                errors.Add($"{midYearPending} developer(s) have not submitted their mid-year review.");
        }
        if (!cycle.FinalReviewReleased)
            errors.Add("The year-end (final) review has not been released.");
        var reviewsPending = active.Count(r => r.Reviewers.Count > 0 && r.Status != ReviewStatus.Completed);
        if (reviewsPending > 0)
            errors.Add($"{reviewsPending} review(s) still have pending manager/peer assessments.");

        if (errors.Count > 0)
            return BadRequest(new { message = "The cycle cannot be ended yet.", errors });

        cycle.Ended = true;
        cycle.EndedAt = DateTime.UtcNow;
        cycle.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { ended = true });
    }
}
