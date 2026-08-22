namespace PlanReview.Api.Models;

public enum UserType
{
    Developer = 0,
    Manager = 1,
    Admin = 2
}

public enum GoalType
{
    Professional = 0,
    Personal = 1
}

public enum GoalStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Dropped = 3
}

public enum ReviewStatus
{
    /// <summary>Cycle released to the developer; plan not yet submitted.</summary>
    Draft = 0,
    /// <summary>Developer has submitted the annual plan at the start of the cycle.</summary>
    Submitted = 1,
    /// <summary>Admin has assigned reviewers; awaiting their assessments.</summary>
    InReview = 2,
    /// <summary>All assigned reviewers have submitted their assessments.</summary>
    Completed = 3
}

public enum ReviewerType
{
    Manager = 0,
    Peer = 1
}

public enum NotificationType
{
    PlanReleased = 0,
    HalfYearlyReleased = 1,
    ReviewerAssigned = 2,
    Reminder = 3
}
