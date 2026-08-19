using System.Security.Claims;

namespace PlanReview.Api.Services;

public static class ClaimsExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(id, out var uid)
            ? uid
            : throw new UnauthorizedAccessException("Missing user id claim.");
    }
}
