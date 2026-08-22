using PlanReview.Api.Data;
using PlanReview.Api.Models;

namespace PlanReview.Api.Services;

/// <summary>
/// Records in-app notifications and (when email is enabled) emails them.
/// Callers add via these helpers and then SaveChanges once.
/// </summary>
public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;

    public NotificationService(AppDbContext db, IEmailSender email)
    {
        _db = db;
        _email = email;
    }

    private static string Date(DateTime? d) => d?.ToString("dd MMM yyyy") ?? "TBD";

    private async Task AddAsync(User recipient, NotificationType type, string subject, string body,
        int? cycleId = null, int? reviewId = null)
    {
        var sent = await _email.SendAsync(recipient.Email, subject, body);
        _db.Notifications.Add(new Notification
        {
            RecipientId = recipient.Id,
            Type = type,
            Subject = subject,
            Body = body,
            ReviewCycleId = cycleId,
            ReviewId = reviewId,
            EmailSent = sent,
        });
    }

    public Task PlanReleasedAsync(User developer, ReviewCycle cycle)
    {
        var subject = $"Your {cycle.Name} plan is now open";
        var reminders = cycle.DueDate is DateTime due
            ? $"Reminders will be sent on {Date(due.AddDays(-14))} and {Date(due.AddDays(-3))}."
            : "Reminders will follow as the due date approaches.";
        var body =
            $"Hi {developer.FullName},\n\n" +
            $"The {cycle.Name} annual plan & review has been released to you.\n\n" +
            $"• Cycle window: {Date(cycle.StartDate)} – {Date(cycle.EndDate)}\n" +
            $"• Complete & submit your plan by: {Date(cycle.DueDate)}\n" +
            $"{reminders}\n\n" +
            "Please sign in to ARISe to fill in your SMART goals, skill self-ratings and select your peer.\n\n" +
            "— ARISe";
        return AddAsync(developer, NotificationType.PlanReleased, subject, body, cycle.Id);
    }

    public Task HalfYearlyReleasedAsync(User developer, ReviewCycle cycle)
    {
        var subject = $"{cycle.Name}: half-yearly review is open";
        var body =
            $"Hi {developer.FullName},\n\n" +
            $"The mid-year checkpoint for {cycle.Name} is now open.\n\n" +
            $"• Update your goal progress (status, completion %, notes) by: {Date(cycle.HalfYearlyDueDate)}\n" +
            "• Add a short mid-year reflection on how the year is tracking.\n\n" +
            "Manager and peer reviews continue at year-end — this checkpoint is your self-update.\n\n" +
            "— ARISe";
        return AddAsync(developer, NotificationType.HalfYearlyReleased, subject, body, cycle.Id);
    }

    public Task RatingsReleasedAsync(User developer, ReviewCycle cycle)
    {
        var subject = $"{cycle.Name}: your ratings are now available";
        var body =
            $"Hi {developer.FullName},\n\n" +
            $"The {cycle.Name} cycle has been closed and your final ratings have been released.\n\n" +
            "Sign in to ARISe and open My Performance to see your self, peer and manager ratings, " +
            "your overall average and where you land on the performance curve.\n\n" +
            "— ARISe";
        return AddAsync(developer, NotificationType.RatingsReleased, subject, body, cycle.Id);
    }

    public Task ReviewerAssignedAsync(User reviewer, ReviewerType role, Review review, string developerName)
    {
        var cycleName = review.ReviewCycle?.Name ?? "the current cycle";
        var subject = $"You're a {role} reviewer for {developerName}";
        var body =
            $"Hi {reviewer.FullName},\n\n" +
            $"You have been assigned as a {role.ToString().ToLower()} reviewer for {developerName}'s {cycleName} review.\n\n" +
            "Your review and rating are due at year-end. You can view the developer's plan now and add your " +
            "assessment once they have submitted.\n\n" +
            "— ARISe";
        return AddAsync(reviewer, NotificationType.ReviewerAssigned, subject, body, review.ReviewCycleId, review.Id);
    }
}
