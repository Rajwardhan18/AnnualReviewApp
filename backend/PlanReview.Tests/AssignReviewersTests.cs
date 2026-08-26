using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PlanReview.Api.Controllers;
using PlanReview.Api.Data;
using PlanReview.Api.DTOs;
using PlanReview.Api.Models;
using PlanReview.Api.Services;

namespace PlanReview.Tests;

/// <summary>
/// The admin picks Manager 1 and Manager 2 in two distinct dropdowns, and those slots carry
/// different weights in the final rating (0.30 vs 0.40). Runs against real SQLite because the
/// bug these cover was a relational one: EF translates <c>ids.Contains(u.Id)</c> to
/// <c>WHERE Id IN (...)</c>, which returns rows in the database's order, not the caller's.
/// </summary>
public sealed class AssignReviewersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    // Seeded so that the manager the admin intends as Manager 1 has the HIGHER id.
    // With the ids ascending, a database-ordered result would silently swap the two slots.
    private const int DeveloperId = 1;
    private const int PeerId = 2;
    private const int IntendedManager2Id = 3;
    private const int IntendedManager1Id = 4;
    private const int AdminId = 5;
    private const int ReviewId = 1;

    public AssignReviewersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        Seed();
    }

    private void Seed()
    {
        _db.Users.AddRange(
            new User { Id = DeveloperId, FullName = "Aisha", Email = "aisha@x.com", UserType = UserType.Developer },
            new User { Id = PeerId, FullName = "Gopal", Email = "gopal@x.com", UserType = UserType.Developer },
            new User { Id = IntendedManager2Id, FullName = "Manny", Email = "manny@x.com", UserType = UserType.Manager },
            new User { Id = IntendedManager1Id, FullName = "Morgan", Email = "morgan@x.com", UserType = UserType.Manager },
            new User { Id = AdminId, FullName = "Admin", Email = "admin@x.com", UserType = UserType.Admin });

        _db.ReviewCycles.Add(new ReviewCycle
        {
            Id = 1,
            Name = "FY2026",
            Year = 2026,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            IsReleased = true
        });

        _db.Reviews.Add(new Review { Id = ReviewId, ReviewCycleId = 1, DeveloperId = DeveloperId });
        _db.SaveChanges();
    }

    private ReviewsController BuildController(int callerId)
    {
        var controller = new ReviewsController(_db, new NotificationService(_db, new NullEmailSender()));
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, callerId.ToString()),
            new Claim(ClaimTypes.Role, UserType.Admin.ToString())
        ], "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
        return controller;
    }

    private double WeightOf(int reviewerId) =>
        _db.ReviewReviewers.AsNoTracking().Single(r => r.ReviewId == ReviewId && r.ReviewerId == reviewerId).Weight;

    [Fact]
    public async Task Assign_GivesTheFirstPickedManagerTheManager1Weight()
    {
        var controller = BuildController(AdminId);

        // The admin deliberately picks the higher-id manager into the Manager 1 slot.
        await controller.Assign(ReviewId,
            new AssignReviewersRequest([IntendedManager1Id, IntendedManager2Id], PeerId));

        Assert.Equal(ReviewRules.Manager1Weight, WeightOf(IntendedManager1Id));
        Assert.Equal(ReviewRules.Manager2Weight, WeightOf(IntendedManager2Id));
    }

    [Fact]
    public async Task Assign_HonoursTheOppositeOrderToo()
    {
        var controller = BuildController(AdminId);

        // Same two people, swapped slots — the weights must swap with them.
        await controller.Assign(ReviewId,
            new AssignReviewersRequest([IntendedManager2Id, IntendedManager1Id], PeerId));

        Assert.Equal(ReviewRules.Manager1Weight, WeightOf(IntendedManager2Id));
        Assert.Equal(ReviewRules.Manager2Weight, WeightOf(IntendedManager1Id));
    }

    [Fact]
    public async Task Assign_ReassigningReplacesTheEarlierSlotsRatherThanAccumulating()
    {
        var controller = BuildController(AdminId);

        await controller.Assign(ReviewId, new AssignReviewersRequest([IntendedManager1Id, IntendedManager2Id], PeerId));
        await controller.Assign(ReviewId, new AssignReviewersRequest([IntendedManager2Id, IntendedManager1Id], PeerId));

        Assert.Equal(3, _db.ReviewReviewers.AsNoTracking().Count(r => r.ReviewId == ReviewId));
        Assert.Equal(ReviewRules.Manager1Weight, WeightOf(IntendedManager2Id));
        Assert.Equal(ReviewRules.Manager2Weight, WeightOf(IntendedManager1Id));
    }

    [Fact]
    public async Task Assign_GivesThePeerThePeerWeight()
    {
        var controller = BuildController(AdminId);

        await controller.Assign(ReviewId, new AssignReviewersRequest([IntendedManager1Id, IntendedManager2Id], PeerId));

        Assert.Equal(ReviewRules.PeerWeight, WeightOf(PeerId));
        Assert.Equal(ReviewerType.Peer,
            _db.ReviewReviewers.AsNoTracking().Single(r => r.ReviewerId == PeerId).ReviewerType);
    }

    [Fact]
    public async Task Assign_RejectsAPeerWhoIsTheDeveloperBeingReviewed()
    {
        var controller = BuildController(AdminId);

        var result = await controller.Assign(ReviewId,
            new AssignReviewersRequest([IntendedManager1Id, IntendedManager2Id], DeveloperId));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Assign_RejectsWhenAnAssignedManagerIsNotAManager()
    {
        var controller = BuildController(AdminId);

        var result = await controller.Assign(ReviewId,
            new AssignReviewersRequest([IntendedManager1Id, PeerId], PeerId));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Assign_RejectsWhenTwoManagersAreNotSupplied()
    {
        var controller = BuildController(AdminId);

        var result = await controller.Assign(ReviewId,
            new AssignReviewersRequest([IntendedManager1Id], PeerId));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    /// <summary>Swallows notification mail so the tests exercise assignment only.</summary>
    private sealed class NullEmailSender : IEmailSender
    {
        public Task<bool> SendAsync(string toEmail, string subject, string body, CancellationToken ct = default) =>
            Task.FromResult(false);
    }
}
