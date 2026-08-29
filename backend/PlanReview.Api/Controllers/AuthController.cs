using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.Function)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });
        if (!user.IsActive)
            return Unauthorized(new { message = "Your account has been deactivated. Please contact an administrator." });

        return new AuthResponse(_tokens.CreateToken(user), ToDto(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var id = User.GetUserId();
        var user = await _db.Users
            .Include(u => u.Function)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        return user is null ? NotFound() : ToDto(user);
    }

    /// <summary>
    /// Change the signed-in user's own password. Also clears the first-login
    /// "must change password" flag, so it doubles as the forced reset.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<UserDto>> ChangePassword(ChangePasswordRequest req)
    {
        var id = User.GetUserId();
        var user = await _db.Users
            .Include(u => u.Function)
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Your current password is incorrect." });
        if (BCrypt.Net.BCrypt.Verify(req.NewPassword, user.PasswordHash))
            return BadRequest(new { message = "Your new password must be different from the current one." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.UserType,
        u.FunctionId, u.Function?.Name, u.RoleId, u.Role?.Name, u.IsActive, u.MustChangePassword);
}
