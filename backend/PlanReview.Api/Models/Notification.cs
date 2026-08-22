using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

/// <summary>
/// An in-app (and optionally emailed) notification for a user, e.g. plan released,
/// half-yearly review opened, or reviewer assignment.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    public int RecipientId { get; set; }
    public User? Recipient { get; set; }

    public NotificationType Type { get; set; }

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public int? ReviewCycleId { get; set; }
    public int? ReviewId { get; set; }

    public bool IsRead { get; set; }

    /// <summary>True if this was actually sent over email (only when email is enabled).</summary>
    public bool EmailSent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
