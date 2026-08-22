using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Services;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotificationsController(AppDbContext db) => _db = db;

    // The current user's notifications, newest first.
    [HttpGet("mine")]
    public async Task<IEnumerable<NotificationDto>> Mine()
    {
        var me = User.GetUserId();
        return await _db.Notifications
            .Where(n => n.RecipientId == me)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Type, n.Subject, n.Body,
                n.ReviewCycleId, n.ReviewId, n.IsRead, n.EmailSent, n.CreatedAt))
            .ToListAsync();
    }

    [HttpGet("mine/unread-count")]
    public async Task<ActionResult<object>> UnreadCount()
    {
        var me = User.GetUserId();
        var count = await _db.Notifications.CountAsync(n => n.RecipientId == me && !n.IsRead);
        return Ok(new { count });
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var me = User.GetUserId();
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.RecipientId == me);
        if (n is null) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var me = User.GetUserId();
        var unread = await _db.Notifications.Where(n => n.RecipientId == me && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok(new { updated = unread.Count });
    }
}
