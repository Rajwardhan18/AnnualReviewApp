using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    // Admin: list all users, optionally filtered by type.
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IEnumerable<UserDto>> GetAll([FromQuery] UserType? type)
    {
        var q = _db.Users.Include(u => u.Function).Include(u => u.Role).AsQueryable();
        if (type is not null) q = q.Where(u => u.UserType == type);
        return await q.OrderBy(u => u.FullName).Select(u => ToDto(u)).ToListAsync();
    }

    // Admin: create a user of any type (Developer / Manager / Admin).
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(AdminCreateUserRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "A user with this email already exists." });

        if (req.UserType == UserType.Developer)
        {
            if (req.FunctionId is null || req.RoleId is null)
                return BadRequest(new { message = "Developers must have a function and a role." });

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
            RoleId = req.UserType == UserType.Developer ? req.RoleId : null,
            // Admin sets an initial password; the user must change it at first login.
            MustChangePassword = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var created = await _db.Users.Include(u => u.Function).Include(u => u.Role)
            .FirstAsync(u => u.Id == user.Id);
        return CreatedAtAction(nameof(GetAll), ToDto(created));
    }

    // Admin: reset the password for any user.
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = "User not found." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        // The admin set a temporary password; force the user to change it at next login.
        user.MustChangePassword = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Password reset for {user.FullName}." });
    }

    // Admin: activate or deactivate a user (deactivated users cannot sign in).
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/active")]
    public async Task<ActionResult<UserDto>> SetActive(int id, SetUserActiveRequest req)
    {
        var user = await _db.Users.Include(u => u.Function).Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { message = "User not found." });
        if (!req.IsActive && user.Id == User.GetUserId())
            return BadRequest(new { message = "You cannot deactivate your own account." });

        user.IsActive = req.IsActive;
        await _db.SaveChangesAsync();
        return ToDto(user);
    }

    // Active managers available for assignment.
    [HttpGet("managers")]
    public async Task<IEnumerable<UserDto>> GetManagers() =>
        await _db.Users.Where(u => u.UserType == UserType.Manager && u.IsActive)
            .OrderBy(u => u.FullName).Select(u => ToDto(u)).ToListAsync();

    // Potential peers for the current developer: other active developers, excluding self.
    [HttpGet("peers")]
    public async Task<IEnumerable<UserDto>> GetPeers()
    {
        var me = User.GetUserId();
        return await _db.Users
            .Include(u => u.Function).Include(u => u.Role)
            .Where(u => u.UserType == UserType.Developer && u.IsActive && u.Id != me)
            .OrderBy(u => u.FullName).Select(u => ToDto(u)).ToListAsync();
    }

    private static UserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.UserType,
        u.FunctionId, u.Function != null ? u.Function.Name : null,
        u.RoleId, u.Role != null ? u.Role.Name : null, u.IsActive, u.MustChangePassword);
}
