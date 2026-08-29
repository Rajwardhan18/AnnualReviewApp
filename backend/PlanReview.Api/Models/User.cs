using System.ComponentModel.DataAnnotations;

namespace PlanReview.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserType UserType { get; set; }

    /// <summary>Deactivated users cannot sign in.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When true, the user must set a new password before using the app.
    /// Set for admin-created accounts and admin password resets.</summary>
    public bool MustChangePassword { get; set; }

    // Developer-only attributes. Null for Managers / Admins.
    public int? FunctionId { get; set; }
    public Function? Function { get; set; }

    public int? RoleId { get; set; }
    public Role? Role { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
