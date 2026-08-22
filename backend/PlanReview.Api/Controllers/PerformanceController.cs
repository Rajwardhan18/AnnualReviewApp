using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/performance")]
[Authorize]
public class PerformanceController : ControllerBase
{
    private readonly AppDbContext _db;
    public PerformanceController(AppDbContext db) => _db = db;

    /// <summary>
    /// The current developer's self-progress and (once released) their ratings and overall
    /// average, with where they land on the cohort's performance curve.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<MyPerformanceDto>> Me()
    {
        var me = User.GetUserId();

        var myReviews = await _db.Reviews
            .Where(r => r.DeveloperId == me)
            .Include(r => r.ReviewCycle)
            .Include(r => r.Goals)
            .Include(r => r.SkillRatings)
            .Include(r => r.Reviewers)
            .Include(r => r.Assessments)
            .OrderByDescending(r => r.ReviewCycle!.Year)
            .ToListAsync();

        // Cohort finals per cycle (for the curve), computed once per cycle.
        var cycleCurves = new Dictionary<int, (double mean, double std)>();
        foreach (var cycleId in myReviews.Select(r => r.ReviewCycleId).Distinct())
        {
            var cohort = await _db.Reviews
                .Where(r => r.ReviewCycleId == cycleId)
                .Include(r => r.SkillRatings)
                .Include(r => r.Reviewers)
                .Include(r => r.Assessments)
                .ToListAsync();
            var finals = cohort.Select(r => RatingCalculator.Compute(r).WeightedFinal)
                .Where(f => f is not null).Select(f => f!.Value).ToList();
            cycleCurves[cycleId] = RatingCalculator.MeanStd(finals);
        }

        var cycles = myReviews.Select(r =>
        {
            var released = r.ReviewCycle?.RatingsReleased ?? false;
            var c = RatingCalculator.Compute(r);

            double? avg = null, pct = null;
            string? band = null;
            double? teamAvg = null;
            if (released)
            {
                var comps = new[] { c.Self, c.Peer, c.Manager1, c.Manager2 }.Where(x => x is not null)
                    .Select(x => x!.Value).ToList();
                avg = comps.Count > 0 ? Math.Round(comps.Average(), 2) : null;

                var (mean, std) = cycleCurves[r.ReviewCycleId];
                teamAvg = Math.Round(mean, 2);
                if (c.WeightedFinal is not null)
                {
                    var z = std > 0 ? (c.WeightedFinal.Value - mean) / std : 0;
                    pct = Math.Round(RatingCalculator.NormalCdf(z) * 100, 1);
                    band = RatingCalculator.Band(z);
                }
            }

            var goals = r.Goals.OrderBy(g => g.GoalType).ThenBy(g => g.Id).ToList();
            var avgCompletion = goals.Count > 0 ? Math.Round(goals.Average(g => (double)g.CompletionPercentage), 0) : 0;

            return new MyPerformanceCycleDto(
                r.Id, r.ReviewCycleId, r.ReviewCycle?.Name ?? "", r.Status, released,
                released ? c.Self : null,
                released ? c.Peer : null,
                released ? c.Manager1 : null,
                released ? c.Manager2 : null,
                released ? c.WeightedFinal : null,
                avg, pct, band, teamAvg,
                goals.Count, avgCompletion,
                goals.Count(g => g.Status == GoalStatus.Completed),
                goals.Count(g => g.Status == GoalStatus.InProgress),
                goals.Count(g => g.Status == GoalStatus.NotStarted),
                goals.Count(g => g.Status == GoalStatus.Dropped),
                goals.Select(g => new MyGoalProgressDto(g.Id, g.Title, g.GoalType, g.Status, g.CompletionPercentage, g.Target)).ToList());
        }).ToList();

        return Ok(new MyPerformanceDto(cycles));
    }
}
