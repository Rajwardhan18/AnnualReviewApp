using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Models;

namespace PlanReview.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Function> Functions => Set<Function>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<RoleSkill> RoleSkills => Set<RoleSkill>();
    public DbSet<CompanyTrait> CompanyTraits => Set<CompanyTrait>();
    public DbSet<ReviewCycle> ReviewCycles => Set<ReviewCycle>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<RndImprovement> RndImprovements => Set<RndImprovement>();
    public DbSet<FutureSkill> FutureSkills => Set<FutureSkill>();
    public DbSet<SkillRating> SkillRatings => Set<SkillRating>();
    public DbSet<ReviewReviewer> ReviewReviewers => Set<ReviewReviewer>();
    public DbSet<ReviewerAssessment> ReviewerAssessments => Set<ReviewerAssessment>();
    public DbSet<ReviewerSkillRating> ReviewerSkillRatings => Set<ReviewerSkillRating>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

        b.Entity<User>()
            .HasOne(u => u.Function).WithMany()
            .HasForeignKey(u => u.FunctionId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<User>()
            .HasOne(u => u.Role).WithMany()
            .HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<Role>()
            .HasOne(r => r.Function).WithMany(f => f.Roles)
            .HasForeignKey(r => r.FunctionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<RoleSkill>()
            .HasOne(rs => rs.Role).WithMany(r => r.RoleSkills)
            .HasForeignKey(rs => rs.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RoleSkill>()
            .HasOne(rs => rs.Skill).WithMany(s => s.RoleSkills)
            .HasForeignKey(rs => rs.SkillId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RoleSkill>().HasIndex(rs => new { rs.RoleId, rs.SkillId }).IsUnique();

        // A developer has at most one review per cycle.
        b.Entity<Review>().HasIndex(r => new { r.ReviewCycleId, r.DeveloperId }).IsUnique();

        b.Entity<Review>()
            .HasOne(r => r.Developer).WithMany()
            .HasForeignKey(r => r.DeveloperId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Review>()
            .HasOne(r => r.SelectedPeer).WithMany()
            .HasForeignKey(r => r.SelectedPeerId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Review>()
            .HasOne(r => r.ReviewCycle).WithMany(c => c.Reviews)
            .HasForeignKey(r => r.ReviewCycleId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Goal>()
            .HasOne(g => g.Review).WithMany(r => r.Goals)
            .HasForeignKey(g => g.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Goal>()
            .HasOne(g => g.CompanyTrait).WithMany()
            .HasForeignKey(g => g.CompanyTraitId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Achievement>()
            .HasOne(a => a.Review).WithMany(r => r.Achievements)
            .HasForeignKey(a => a.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Achievement>()
            .HasOne(a => a.CompanyTrait).WithMany()
            .HasForeignKey(a => a.CompanyTraitId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<RndImprovement>()
            .HasOne(r => r.Review).WithMany(rv => rv.RndImprovements)
            .HasForeignKey(r => r.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<FutureSkill>()
            .HasOne(f => f.Review).WithMany(rv => rv.FutureSkills)
            .HasForeignKey(f => f.ReviewId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<SkillRating>()
            .HasOne(sr => sr.Review).WithMany(r => r.SkillRatings)
            .HasForeignKey(sr => sr.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SkillRating>()
            .HasOne(sr => sr.Skill).WithMany()
            .HasForeignKey(sr => sr.SkillId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<ReviewReviewer>()
            .HasOne(rr => rr.Review).WithMany(r => r.Reviewers)
            .HasForeignKey(rr => rr.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ReviewReviewer>()
            .HasOne(rr => rr.Reviewer).WithMany()
            .HasForeignKey(rr => rr.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReviewReviewer>().HasIndex(rr => new { rr.ReviewId, rr.ReviewerId }).IsUnique();

        b.Entity<ReviewerAssessment>()
            .HasOne(a => a.Review).WithMany(r => r.Assessments)
            .HasForeignKey(a => a.ReviewId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ReviewerAssessment>()
            .HasOne(a => a.Reviewer).WithMany()
            .HasForeignKey(a => a.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ReviewerAssessment>().HasIndex(a => new { a.ReviewId, a.ReviewerId }).IsUnique();

        b.Entity<ReviewerSkillRating>()
            .HasOne(sr => sr.ReviewerAssessment).WithMany(a => a.SkillRatings)
            .HasForeignKey(sr => sr.ReviewerAssessmentId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<ReviewerSkillRating>()
            .HasOne(sr => sr.Skill).WithMany()
            .HasForeignKey(sr => sr.SkillId).OnDelete(DeleteBehavior.Restrict);
    }
}
