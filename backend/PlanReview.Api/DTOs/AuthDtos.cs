using System.ComponentModel.DataAnnotations;
using PlanReview.Api.Models;

namespace PlanReview.Api.DTOs;

public record RegisterRequest(
    [Required] string FullName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] UserType UserType,
    // Required when UserType == Developer.
    int? FunctionId,
    int? RoleId);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

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
    string? RoleName);
