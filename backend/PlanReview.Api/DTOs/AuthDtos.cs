using System.ComponentModel.DataAnnotations;
using PlanReview.Api.Models;

namespace PlanReview.Api.DTOs;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

/// <summary>Self-service password change (also used for the forced first-login reset).</summary>
public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(6)] string NewPassword);

/// <summary>Admin-driven password reset for any user.</summary>
public record ResetPasswordRequest([Required, MinLength(6)] string NewPassword);

/// <summary>Admin-driven user creation. Unlike self-registration, this allows
/// creating Managers, Admins, and Developers (with function + role).</summary>
public record AdminCreateUserRequest(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] UserType UserType,
    int? FunctionId,
    int? RoleId);

public record AuthResponse(
    string Token,
    UserDto User);

public record UserDto(
    int Id,
    string FullName,
    string Email,
    UserType UserType,
    int? FunctionId,
    string? FunctionName,
    int? RoleId,
    string? RoleName,
    bool IsActive,
    bool MustChangePassword);

/// <summary>Admin toggles a user's active state.</summary>
public record SetUserActiveRequest(bool IsActive);
