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

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "A user with this email already exists." });

        // Admin accounts cannot be self-registered.
        if (req.UserType == UserType.Admin)
            return BadRequest(new { message = "Admin accounts cannot be self-registered." });

        if (req.UserType == UserType.Developer)
        {
            if (req.FunctionId is null || req.RoleId is null)
                return BadRequest(new { message = "Developers must select a function and a role." });

            var role = await _db.Roles.FindAsync(req.RoleId.Value);
            if (role is null || role.FunctionId != req.FunctionId.Value)
                return BadRequest(new { message = "The selected role does not belong to the selected function." });
        }

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            UserType = req.UserType,
            FunctionId = req.UserType == UserType.Developer ? req.FunctionId : null,
            RoleId = req.UserType == UserType.Developer ? req.RoleId : null
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await BuildAuthResponse(user.Id);
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

    private async Task<ActionResult<AuthResponse>> BuildAuthResponse(int userId)
    {
        var user = await _db.Users
            .Include(u => u.Function)
            .Include(u => u.Role)
            .FirstAsync(u => u.Id == userId);
        return new AuthResponse(_tokens.CreateToken(user), ToDto(user));
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.UserType,
        u.FunctionId, u.Function?.Name, u.RoleId, u.Role?.Name, u.IsActive);
}
