using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    /// <summary>
    /// Developer ratings for a cycle: self / peer / manager-1 / manager-2 component scores,
    /// the weighted normalized final, and each developer's fit on the cohort's normal curve.
    /// </summary>
    [HttpGet("ratings")]
    public async Task<ActionResult<RatingsDashboard>> Ratings([FromQuery] int? cycleId)
    {
        var cycle = cycleId is not null
            ? await _db.ReviewCycles.FindAsync(cycleId.Value)
            : await _db.ReviewCycles.Where(c => c.IsActive).OrderByDescending(c => c.Year).FirstOrDefaultAsync()
              ?? await _db.ReviewCycles.OrderByDescending(c => c.Year).FirstOrDefaultAsync();

        if (cycle is null)
            return Ok(new RatingsDashboard(0, "—", Weights(), EmptyCurve(), new()));

        var reviews = await _db.Reviews
            .Where(r => r.ReviewCycleId == cycle.Id)
            .Include(r => r.Developer)
            .Include(r => r.Function)
            .Include(r => r.Role)
            .Include(r => r.SkillRatings)
            .Include(r => r.Reviewers)
            .Include(r => r.Assessments)
            .ToListAsync();

        // First pass: component scores + weighted final.
        var prelim = reviews.Select(r =>
        {
            var c = RatingCalculator.Compute(r);
            return new
            {
                Review = r,
                c.Self, c.Peer, c.Manager1, c.Manager2, c.WeightedFinal,
            };
        }).ToList();

        // Fit the cohort of finals onto a normal curve.
        var finals = prelim.Where(p => p.WeightedFinal is not null).Select(p => p.WeightedFinal!.Value).ToList();
        var (mean, std) = RatingCalculator.MeanStd(finals);

        var rows = prelim
            .Select(p =>
            {
                double? z = null, pct = null, curved = null;
                string? band = null;
                if (p.WeightedFinal is not null)
                {
                    z = std > 0 ? Math.Round((p.WeightedFinal.Value - mean) / std, 2) : 0;
                    pct = Math.Round(RatingCalculator.NormalCdf(z.Value) * 100, 1);
                    curved = Math.Clamp(Math.Round(5.5 + 1.5 * z.Value, 1), 1, 10);
                    band = RatingCalculator.Band(z.Value);
                }
                var r = p.Review;
                return new DeveloperRatingRow(
                    r.Id, r.DeveloperId, r.Developer?.FullName ?? "",
                    r.Function?.Name, r.Role?.Name,
                    cycle.Id, cycle.Name, r.Status,
                    Round(p.Self), Round(p.Peer), Round(p.Manager1), Round(p.Manager2),
                    p.WeightedFinal, z, pct, curved, band);
            })
            .OrderByDescending(r => r.WeightedFinal ?? -1)
            .ThenBy(r => r.DeveloperName)
            .ToList();

        var buckets = RatingCalculator.BandRanges.Select(br =>
        {
            var count = rows.Count(r => r.ZScore is not null && r.ZScore >= br.Lower && r.ZScore < br.Upper);
            return new BandBucket(br.Band, count, Finite(br.Lower), Finite(br.Upper));
        }).ToList();

        var curve = new CurveStats(
            finals.Count,
            Math.Round(mean, 2),
            Math.Round(std, 2),
            finals.Count > 0 ? Math.Round(finals.Min(), 2) : 0,
            finals.Count > 0 ? Math.Round(finals.Max(), 2) : 0,
            buckets);

        return Ok(new RatingsDashboard(cycle.Id, cycle.Name, Weights(), curve, rows));
    }

    private static double? Round(double? v) => v is null ? null : Math.Round(v.Value, 2);
    private static double? Finite(double v) => double.IsInfinity(v) ? null : v;
    private static RatingWeights Weights() => new(
        ReviewRules.SelfWeight, ReviewRules.PeerWeight, ReviewRules.Manager1Weight, ReviewRules.Manager2Weight);
    private static CurveStats EmptyCurve() => new(0, 0, 0, 0, 0,
        RatingCalculator.BandRanges.Select(br => new BandBucket(br.Band, 0, Finite(br.Lower), Finite(br.Upper))).ToList());
}
