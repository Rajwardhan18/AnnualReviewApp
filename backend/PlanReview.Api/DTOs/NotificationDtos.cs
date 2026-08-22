using PlanReview.Api.Models;

namespace PlanReview.Api.DTOs;

public record NotificationDto(
    int Id,
    NotificationType Type,
    string Subject,
    string Body,
    int? ReviewCycleId,
    int? ReviewId,
    bool IsRead,
    bool EmailSent,
    DateTime CreatedAt);
