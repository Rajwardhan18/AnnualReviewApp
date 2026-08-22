using Microsoft.EntityFrameworkCore;
using PlanReview.Api.Models;

namespace PlanReview.Api.Data;

/// <summary>
/// Seeds master data (functions, roles, skills, role-skill maps, company traits)
/// and a default admin account on first run.
/// </summary>
public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@company.com";
    public const string DefaultAdminPassword = "Admin@123";

    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync(u => u.UserType == UserType.Admin))
        {
            db.Users.Add(new User
            {
                FullName = "System Administrator",
                Email = DefaultAdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultAdminPassword),
                UserType = UserType.Admin
            });
        }

        if (!await db.CompanyTraits.AnyAsync())
        {
            db.CompanyTraits.AddRange(
                new CompanyTrait { Name = "Leadership", Description = "Guiding, influencing and inspiring others." },
                new CompanyTrait { Name = "Ownership", Description = "Taking end-to-end accountability for outcomes." },
                new CompanyTrait { Name = "Integrity", Description = "Being honest and doing the right thing." },
                new CompanyTrait { Name = "Customer Focus", Description = "Obsessing over customer value." },
                new CompanyTrait { Name = "Innovation", Description = "Finding better ways of solving problems." },
                new CompanyTrait { Name = "Collaboration", Description = "Working effectively across teams." });
        }

        if (!await db.Functions.AnyAsync())
        {
            var frontend = new Function { Name = "Frontend Developer", Description = "Builds user-facing interfaces." };
            var backend = new Function { Name = "Backend Developer", Description = "Builds services, APIs and data layers." };

            // Skills (master).
            Skill Sk(string n, string c) => new() { Name = n, Category = c };
            var react = Sk("React", "Frontend");
            var ts = Sk("TypeScript", "Frontend");
            var css = Sk("HTML/CSS", "Frontend");
            var uiux = Sk("UI/UX Fundamentals", "Frontend");
            var testingFe = Sk("Frontend Testing", "Frontend");
            var perf = Sk("Web Performance", "Frontend");

            var csharp = Sk("C#", "Backend");
            var efcore = Sk("Entity Framework / ORM", "Backend");
            var sql = Sk("SQL & Databases", "Backend");
            var apis = Sk("REST API Design", "Backend");
            var sysdesign = Sk("System Design", "Backend");
            var security = Sk("Application Security", "Backend");

            db.Skills.AddRange(react, ts, css, uiux, testingFe, perf,
                               csharp, efcore, sql, apis, sysdesign, security);

            // Roles per function.
            Role R(string n, Function f) => new() { Name = n, Function = f };
            var feSde1 = R("SDE-1", frontend);
            var feSde2 = R("SDE-2", frontend);
            var feSde3 = R("SDE-3 / Senior", frontend);
            var beSde1 = R("SDE-1", backend);
            var beSde2 = R("SDE-2", backend);
            var beSde3 = R("SDE-3 / Senior", backend);

            db.Functions.AddRange(frontend, backend);

            // Role -> skill maps. Higher roles inherit the lower-role skills + more.
            void Map(Role r, params Skill[] skills)
            {
                foreach (var s in skills)
                    db.RoleSkills.Add(new RoleSkill { Role = r, Skill = s });
            }

            Map(feSde1, react, ts, css);
            Map(feSde2, react, ts, css, uiux, testingFe);
            Map(feSde3, react, ts, css, uiux, testingFe, perf);

            Map(beSde1, csharp, sql, apis);
            Map(beSde2, csharp, efcore, sql, apis);
            Map(beSde3, csharp, efcore, sql, apis, sysdesign, security);
        }

        if (!await db.ReviewCycles.AnyAsync())
        {
            db.ReviewCycles.Add(new ReviewCycle
            {
                Name = "FY2026 Annual Cycle",
                Year = 2026,
                StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                IsReleased = false
            });
        }

        await db.SaveChangesAsync();

        // Backfill a plan due date on any active cycle that doesn't have one yet.
        var activeCycle = await db.ReviewCycles.FirstOrDefaultAsync(c => c.IsActive && c.DueDate == null);
        if (activeCycle is not null)
        {
            activeCycle.DueDate = new DateTime(activeCycle.Year, 2, 15, 0, 0, 0, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }
    }
}
