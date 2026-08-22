using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notify;
    public ReviewsController(AppDbContext db, NotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    // ---------------- Lists ----------------

    // Developer: my reviews across cycles.
    [HttpGet("mine")]
    public async Task<IEnumerable<ReviewSummaryDto>> Mine()
    {
        var me = User.GetUserId();
        var rows = await _db.Reviews
            .Where(r => r.DeveloperId == me)
            .Include(r => r.ReviewCycle).Include(r => r.Developer)
            .Include(r => r.Function).Include(r => r.Role)
            .OrderByDescending(r => r.ReviewCycle!.Year)
            .ToListAsync();
        return rows.Select(ToSummary);
    }

    // Reviewer (manager/peer): reviews assigned to me that have been submitted.
    [HttpGet("assigned")]
    public async Task<IEnumerable<ReviewSummaryDto>> Assigned()
    {
        var me = User.GetUserId();
        var rows = await _db.Reviews
            .Where(r => r.Reviewers.Any(rr => rr.ReviewerId == me)
                        && r.Status != ReviewStatus.Draft)
            .Include(r => r.ReviewCycle).Include(r => r.Developer)
            .Include(r => r.Function).Include(r => r.Role)
            .OrderByDescending(r => r.ReviewCycle!.Year)
            .ToListAsync();
        return rows.Select(ToSummary);
    }

    // Admin: all reviews.
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IEnumerable<ReviewSummaryDto>> All([FromQuery] int? cycleId)
    {
        var q = _db.Reviews.AsQueryable();
        if (cycleId is not null) q = q.Where(r => r.ReviewCycleId == cycleId);
        var rows = await q
            .Include(r => r.ReviewCycle).Include(r => r.Developer)
            .Include(r => r.Function).Include(r => r.Role)
            .OrderByDescending(r => r.ReviewCycle!.Year).ThenBy(r => r.Developer!.FullName)
            .ToListAsync();
        return rows.Select(ToSummary);
    }

    // ---------------- Detail ----------------

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReviewDetailDto>> Get(int id)
    {
        var review = await LoadFull(id);
        if (review is null) return NotFound();
        if (!await CanAccess(review)) return Forbid();
        return await BuildDetail(review);
    }

    // ---------------- Save plan (draft) ----------------

    [HttpPut("{id:int}/plan")]
    public async Task<ActionResult<ReviewDetailDto>> SavePlan(int id, SavePlanRequest req)
    {
        var review = await LoadFull(id);
        if (review is null) return NotFound();

        var me = User.GetUserId();
        if (review.DeveloperId != me)
            return Forbid();
        if (review.Status != ReviewStatus.Draft)
            return BadRequest(new { message = "This review has already been submitted and can no longer be edited." });

        // Validate the selected peer is a different developer.
        if (req.SelectedPeerId is not null)
        {
            var peer = await _db.Users.FindAsync(req.SelectedPeerId.Value);
            if (peer is null || peer.UserType != UserType.Developer || peer.Id == me)
                return BadRequest(new { message = "The selected peer must be another developer." });
        }

        // Draft-friendly validation: only referenced traits/skills that ARE provided must be valid.
        var traitIds = (await _db.CompanyTraits.Select(t => t.Id).ToListAsync()).ToHashSet();
        foreach (var g in req.Goals)
            if (g.CompanyTraitId is not null && !traitIds.Contains(g.CompanyTraitId.Value))
                return BadRequest(new { message = $"Goal \"{g.Title}\" references an unknown company trait." });
        foreach (var a in req.Achievements ?? new())
            if (a.CompanyTraitId is not null && !traitIds.Contains(a.CompanyTraitId.Value))
                return BadRequest(new { message = "An achievement references an unknown company trait." });

        var roleSkillIds = review.RoleId is null
            ? new HashSet<int>()
            : (await _db.RoleSkills.Where(rs => rs.RoleId == review.RoleId)
                .Select(rs => rs.SkillId).ToListAsync()).ToHashSet();
        foreach (var sr in req.SkillRatings)
            if (!roleSkillIds.Contains(sr.SkillId))
                return BadRequest(new { message = "A skill rating references a skill not mapped to this role." });

        // Replace goals, achievements, R&D, future skills and skill ratings.
        _db.Goals.RemoveRange(review.Goals);
        _db.Achievements.RemoveRange(review.Achievements);
        _db.RndImprovements.RemoveRange(review.RndImprovements);
        _db.FutureSkills.RemoveRange(review.FutureSkills);
        _db.SkillRatings.RemoveRange(review.SkillRatings);

        review.SelectedPeerId = req.SelectedPeerId;
        review.SelfSummary = req.SelfSummary;

        foreach (var g in req.Goals)
        {
            review.Goals.Add(new Goal
            {
                GoalType = g.GoalType,
                Title = (g.Title ?? "").Trim(),
                Specific = g.Specific ?? "",
                Measurable = g.Measurable ?? "",
                Achievable = g.Achievable ?? "",
                Relevant = g.Relevant ?? "",
                TimeBound = g.TimeBound ?? "",
                CompanyTraitId = g.CompanyTraitId,
                Status = g.Status ?? GoalStatus.NotStarted,
                CompletionPercentage = Math.Clamp(g.CompletionPercentage ?? 0, 0, 100),
                StatusComment = g.StatusComment,
                StatusDate = g.StatusDate
            });
        }
        // Only persist achievements that have a project name or a description.
        foreach (var a in (req.Achievements ?? new())
                     .Where(a => !string.IsNullOrWhiteSpace(a.ProjectName) || !string.IsNullOrWhiteSpace(a.WorkDescription)))
        {
            review.Achievements.Add(new Achievement
            {
                ProjectName = (a.ProjectName ?? "").Trim(),
                ClientName = (a.ClientName ?? "").Trim(),
                WorkDescription = (a.WorkDescription ?? "").Trim(),
                ManagerRating = a.ManagerRating,
                CompanyTraitId = a.CompanyTraitId
            });
        }
        foreach (var r in (req.RndImprovements ?? new()).Where(x => !string.IsNullOrWhiteSpace(x.Description)))
            review.RndImprovements.Add(new RndImprovement { Description = r.Description!.Trim() });
        foreach (var f in (req.FutureSkills ?? new()).Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            review.FutureSkills.Add(new FutureSkill { Name = f.Name!.Trim() });
        foreach (var sr in req.SkillRatings)
        {
            review.SkillRatings.Add(new SkillRating
            {
                SkillId = sr.SkillId,
                SelfRating = sr.SelfRating,
                Comments = sr.Comments
            });
        }

        await _db.SaveChangesAsync();
        var reloaded = await LoadFull(id);
        return await BuildDetail(reloaded!);
    }

    // ---------------- Submit ----------------

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<ReviewDetailDto>> Submit(int id)
    {
        var review = await LoadFull(id);
        if (review is null) return NotFound();

        var me = User.GetUserId();
        if (review.DeveloperId != me) return Forbid();
        if (review.Status != ReviewStatus.Draft)
            return BadRequest(new { message = "This review has already been submitted." });

        var errors = await ValidateForSubmit(review);
        if (errors.Count > 0)
            return BadRequest(new { message = "The plan is incomplete.", errors });

        // Requirement 7: the developer-selected peer becomes an actual Peer reviewer on submit
        // so their peer review can be captured (and shown to the admin) without waiting for
        // admin assignment. The admin can still reassign later.
        if (review.SelectedPeerId is not null &&
            !review.Reviewers.Any(rr => rr.ReviewerId == review.SelectedPeerId.Value))
        {
            review.Reviewers.Add(new ReviewReviewer
            {
                ReviewerId = review.SelectedPeerId.Value,
                ReviewerType = ReviewerType.Peer,
                Weight = ReviewRules.PeerWeight
            });
        }

        // If any reviewers are assigned, go straight to InReview.
        review.Status = review.Reviewers.Count > 0 ? ReviewStatus.InReview : ReviewStatus.Submitted;
        review.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var reloaded = await LoadFull(id);
        return await BuildDetail(reloaded!);
    }

    // ---------------- Developer: update goal progress (any time) ----------------

    /// <summary>
    /// Updates the status / completion % / comment / date for the developer's goals.
    /// Allowed in any review status so progress can be tracked through the year.
    /// </summary>
    [HttpPut("{id:int}/progress")]
    public async Task<ActionResult<ReviewDetailDto>> SaveProgress(int id, SaveProgressRequest req)
    {
        var review = await LoadFull(id);
        if (review is null) return NotFound();

        var me = User.GetUserId();
        if (review.DeveloperId != me) return Forbid();

        foreach (var p in req.Goals)
        {
            var goal = review.Goals.FirstOrDefault(g => g.Id == p.GoalId);
            if (goal is null) continue;
            goal.Status = p.Status;
            goal.CompletionPercentage = Math.Clamp(p.CompletionPercentage, 0, 100);
            goal.StatusComment = p.StatusComment;
            goal.StatusDate = p.StatusDate;
        }

        if (req.MidYearReflection is not null)
        {
            review.MidYearReflection = req.MidYearReflection;
            review.MidYearUpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return await BuildDetail((await LoadFull(id))!);
    }

    // ---------------- Admin: assign reviewers (2 managers + 1 peer) ----------------

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<ReviewDetailDto>> Assign(int id, AssignReviewersRequest req)
    {
        var review = await LoadFull(id);
        if (review is null) return NotFound();

        // Reviewers may be assigned at any time (even before the developer submits).
        var managerIds = req.ManagerIds.Distinct().ToList();
        if (managerIds.Count != ReviewRules.RequiredManagers)
            return BadRequest(new { message = $"Exactly {ReviewRules.RequiredManagers} managers must be assigned." });

        var managers = await _db.Users.Where(u => managerIds.Contains(u.Id)).ToListAsync();
        if (managers.Count != managerIds.Count || managers.Any(m => m.UserType != UserType.Manager))
            return BadRequest(new { message = "All assigned managers must be valid Manager users." });

        var peer = await _db.Users.FindAsync(req.PeerId);
        if (peer is null || peer.UserType != UserType.Developer || peer.Id == review.DeveloperId)
            return BadRequest(new { message = "The assigned peer must be another developer." });

        // Replace reviewer assignments, assigning weights. The first manager picked
        // carries 0.30, the second 0.40, the peer 0.20 (self-rating carries 0.10).
        _db.ReviewReviewers.RemoveRange(review.Reviewers);
        var managerWeights = new[] { ReviewRules.Manager1Weight, ReviewRules.Manager2Weight };
        for (var i = 0; i < managers.Count; i++)
            review.Reviewers.Add(new ReviewReviewer
            {
                ReviewerId = managers[i].Id,
                ReviewerType = ReviewerType.Manager,
                Weight = managerWeights[i]
            });
        review.Reviewers.Add(new ReviewReviewer
        {
            ReviewerId = peer.Id,
            ReviewerType = ReviewerType.Peer,
            Weight = ReviewRules.PeerWeight
        });

        // If the plan is already submitted, move it into review. If it's still a Draft,
        // keep it as Draft — reviewers can only submit assessments once the developer submits.
        if (review.Status == ReviewStatus.Submitted)
            review.Status = ReviewStatus.InReview;

        // Requirement 4: notify the assigned managers and peer.
        var developerName = review.Developer?.FullName ?? "a developer";
        foreach (var m in managers)
            await _notify.ReviewerAssignedAsync(m, ReviewerType.Manager, review, developerName);
        await _notify.ReviewerAssignedAsync(peer, ReviewerType.Peer, review, developerName);

        await _db.SaveChangesAsync();

        var reloaded = await LoadFull(id);
        return await BuildDetail(reloaded!);
    }

    // ---------------- Reviewer: submit assessment ----------------

    [HttpPost("{id:int}/assessment")]
    public async Task<ActionResult<ReviewDetailDto>> SubmitAssessment(int id, SubmitAssessmentRequest req)
    {
        var review = await LoadFull(id);
        if (review is null) return NotFound();

        var me = User.GetUserId();
        var assignment = review.Reviewers.FirstOrDefault(r => r.ReviewerId == me);
        if (assignment is null)
            return Forbid();
        if (review.Status == ReviewStatus.Draft)
            return BadRequest(new { message = "The developer has not submitted this plan yet." });

        var assessment = review.Assessments.FirstOrDefault(a => a.ReviewerId == me);
        if (assessment is null)
        {
            assessment = new ReviewerAssessment { ReviewId = id, ReviewerId = me, ReviewerType = assignment.ReviewerType };
            review.Assessments.Add(assessment);
        }
        else
        {
            _db.ReviewerSkillRatings.RemoveRange(assessment.SkillRatings);
            assessment.SkillRatings.Clear();
        }

        assessment.OverallRating = req.OverallRating;
        assessment.Strengths = req.Strengths;
        assessment.Improvements = req.Improvements;
        assessment.SubmittedAt = DateTime.UtcNow;

        foreach (var sr in req.SkillRatings)
            assessment.SkillRatings.Add(new ReviewerSkillRating { SkillId = sr.SkillId, Rating = sr.Rating });

        // Mark completed once every assigned reviewer has submitted.
        await _db.SaveChangesAsync();
        var reloaded = await LoadFull(id);
        if (reloaded!.Reviewers.Count > 0 &&
            reloaded.Reviewers.All(rr => reloaded.Assessments.Any(a => a.ReviewerId == rr.ReviewerId && a.SubmittedAt != null)))
        {
            reloaded.Status = ReviewStatus.Completed;
            await _db.SaveChangesAsync();
        }

        return await BuildDetail((await LoadFull(id))!);
    }

    // ================= helpers =================

    private async Task<List<string>> ValidateForSubmit(Review review)
    {
        var errors = new List<string>();

        var professional = review.Goals.Count(g => g.GoalType == GoalType.Professional);
        var personal = review.Goals.Count(g => g.GoalType == GoalType.Personal);

        if (professional < ReviewRules.MinProfessionalGoals)
            errors.Add($"At least {ReviewRules.MinProfessionalGoals} professional goals are required (you have {professional}).");
        if (personal < ReviewRules.MinPersonalGoals)
            errors.Add($"At least {ReviewRules.MinPersonalGoals} personal goals are required (you have {personal}).");

        if (review.SelectedPeerId is null)
            errors.Add("You must select a peer for the review.");

        foreach (var g in review.Goals)
        {
            var label = string.IsNullOrWhiteSpace(g.Title) ? "(untitled)" : g.Title;
            if (string.IsNullOrWhiteSpace(g.Title) || string.IsNullOrWhiteSpace(g.Specific) ||
                string.IsNullOrWhiteSpace(g.Measurable) || string.IsNullOrWhiteSpace(g.Achievable) ||
                string.IsNullOrWhiteSpace(g.Relevant) || string.IsNullOrWhiteSpace(g.TimeBound))
                errors.Add($"Goal \"{label}\" is missing one or more SMART fields.");
            if (g.CompanyTraitId is null)
                errors.Add($"Goal \"{label}\" must be tagged against a company trait.");
        }

        // Every skill identified for the role must be rated.
        if (review.RoleId is not null)
        {
            var roleSkillIds = await _db.RoleSkills.Where(rs => rs.RoleId == review.RoleId)
                .Select(rs => rs.SkillId).ToListAsync();
            var ratedIds = review.SkillRatings.Select(sr => sr.SkillId).ToHashSet();
            var missing = roleSkillIds.Where(sid => !ratedIds.Contains(sid)).ToList();
            if (missing.Count > 0)
                errors.Add($"You must rate all {roleSkillIds.Count} skills identified for your role ({missing.Count} unrated).");
        }

        return errors;
    }

    private Task<Review?> LoadFull(int id) =>
        _db.Reviews
            .Include(r => r.ReviewCycle)
            .Include(r => r.Developer)
            .Include(r => r.Function)
            .Include(r => r.Role)
            .Include(r => r.SelectedPeer)
            .Include(r => r.Goals).ThenInclude(g => g.CompanyTrait)
            .Include(r => r.Achievements).ThenInclude(a => a.CompanyTrait)
            .Include(r => r.RndImprovements)
            .Include(r => r.FutureSkills)
            .Include(r => r.SkillRatings).ThenInclude(sr => sr.Skill)
            .Include(r => r.Reviewers).ThenInclude(rr => rr.Reviewer)
            .Include(r => r.Assessments).ThenInclude(a => a.Reviewer)
            .Include(r => r.Assessments).ThenInclude(a => a.SkillRatings).ThenInclude(sr => sr.Skill)
            .FirstOrDefaultAsync(r => r.Id == id);

    private async Task<bool> CanAccess(Review review)
    {
        if (User.IsInRole(UserType.Admin.ToString())) return true;
        var me = User.GetUserId();
        if (review.DeveloperId == me) return true;
        if (review.Reviewers.Any(rr => rr.ReviewerId == me) && review.Status != ReviewStatus.Draft) return true;
        await Task.CompletedTask;
        return false;
    }

    private async Task<ReviewDetailDto> BuildDetail(Review r)
    {
        var roleSkills = r.RoleId is null
            ? new List<SkillDto>()
            : await _db.RoleSkills.Where(rs => rs.RoleId == r.RoleId)
                .Include(rs => rs.Skill)
                .OrderBy(rs => rs.Skill!.Category).ThenBy(rs => rs.Skill!.Name)
                .Select(rs => new SkillDto(rs.Skill!.Id, rs.Skill.Name, rs.Skill.Category))
                .ToListAsync();

        return new ReviewDetailDto(
            r.Id,
            r.ReviewCycleId,
            r.ReviewCycle?.Name ?? "",
            r.DeveloperId,
            r.Developer?.FullName ?? "",
            r.FunctionId,
            r.Function?.Name,
            r.RoleId,
            r.Role?.Name,
            r.Status,
            r.SubmittedAt,
            r.SelectedPeerId,
            r.SelectedPeer?.FullName,
            r.SelfSummary,
            r.MidYearReflection,
            r.ReviewCycle?.HalfYearlyReleased ?? false,
            r.ReviewCycle?.HalfYearlyDueDate,
            r.ReviewCycle?.DueDate,
            r.Goals.Select(g => new GoalDto(g.Id, g.GoalType, g.Title, g.Specific, g.Measurable,
                g.Achievable, g.Relevant, g.TimeBound, g.CompanyTraitId, g.CompanyTrait?.Name,
                g.Status, g.CompletionPercentage, g.StatusComment, g.StatusDate)).ToList(),
            r.Achievements.Select(a => new AchievementDto(a.Id, a.ProjectName, a.ClientName, a.WorkDescription,
                a.ManagerRating, a.CompanyTraitId, a.CompanyTrait?.Name)).ToList(),
            r.RndImprovements.Select(x => new RndImprovementDto(x.Id, x.Description)).ToList(),
            r.FutureSkills.Select(x => new FutureSkillDto(x.Id, x.Name)).ToList(),
            r.SkillRatings.Select(sr => new SkillRatingDto(sr.SkillId, sr.Skill?.Name ?? "", sr.SelfRating, sr.Comments)).ToList(),
            roleSkills,
            r.Reviewers.Select(rr => new ReviewerDto(rr.ReviewerId, rr.Reviewer?.FullName ?? "", rr.ReviewerType,
                r.Assessments.Any(a => a.ReviewerId == rr.ReviewerId && a.SubmittedAt != null))).ToList(),
            r.Assessments.Select(a => new AssessmentDto(a.Id, a.ReviewerId, a.Reviewer?.FullName ?? "", a.ReviewerType,
                a.OverallRating, a.Strengths, a.Improvements, a.SubmittedAt,
                a.SkillRatings.Select(sr => new ReviewerSkillRatingDto(sr.SkillId, sr.Skill?.Name ?? "", sr.Rating)).ToList())).ToList());
    }

    private static ReviewSummaryDto ToSummary(Review r) => new(
        r.Id, r.ReviewCycleId, r.ReviewCycle!.Name, r.DeveloperId, r.Developer!.FullName,
        r.Function != null ? r.Function.Name : null,
        r.Role != null ? r.Role.Name : null,
        r.Status, r.SubmittedAt);
}
