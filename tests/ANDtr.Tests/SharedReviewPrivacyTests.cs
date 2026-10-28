using System.Net;
using ANDtr.Web.Data;
using ANDtr.Web.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ANDtr.Tests;

// Customer privacy through the real pages (A2-05, A2-06): a review shared with Org A is visible to
// Org A customers only. Org B customers get Access denied and none of the review's data.
public sealed class SharedReviewPrivacyTests(WebAppFactory factory) : IClassFixture<WebAppFactory>, IAsyncLifetime
{
    private const string ReviewName = "Privacy test review";
    private const string VisibleComment = "Please check your serial numbers.";
    private const string InternalComment = "Internal only: supplier still investigating.";

    private int _sharedReviewId;
    private int _unsharedReviewId;
    private string _orgACustomer = null!;
    private string _orgBCustomer = null!;

    public async Task InitializeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var orgA = await AddOrganisationAsync($"Org A {suffix}");
        var orgB = await AddOrganisationAsync($"Org B {suffix}");
        _orgACustomer = (await factory.CreateUserAsync($"org-a-{suffix}@test.local", Roles.Customer, orgA)).Email!;
        _orgBCustomer = (await factory.CreateUserAsync($"org-b-{suffix}@test.local", Roles.Customer, orgB)).Email!;
        var employee = await factory.CreateUserAsync($"owner-{suffix}@test.local", Roles.Employee);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var shared = new RiskReview { Name = ReviewName, OwnerId = employee.Id, OrganisationId = orgA, CreatedAt = DateTime.UtcNow, SharedAt = DateTime.UtcNow };
        shared.Comments.Add(new Comment { AuthorId = employee.Id, Body = VisibleComment, CreatedAt = DateTime.UtcNow });
        shared.Comments.Add(new Comment { AuthorId = employee.Id, Body = InternalComment, IsInternal = true, CreatedAt = DateTime.UtcNow });
        var unshared = new RiskReview { Name = "Draft review", OwnerId = employee.Id, CreatedAt = DateTime.UtcNow };
        db.RiskReviews.AddRange(shared, unshared);
        await db.SaveChangesAsync();
        _sharedReviewId = shared.Id;
        _unsharedReviewId = unshared.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ---- A2-05 ----

    [Fact]
    public async Task OrgACustomer_SeesTheReviewInTheirList()
    {
        var client = await factory.CreateSignedInClientAsync(_orgACustomer, WebAppFactory.Password);

        Assert.Contains(ReviewName, await client.GetStringAsync("/SharedReviews"));
    }

    [Fact]
    public async Task OrgACustomer_CanOpenTheReview_WithoutInternalComments()
    {
        var client = await factory.CreateSignedInClientAsync(_orgACustomer, WebAppFactory.Password);

        var response = await client.GetAsync($"/SharedReviews/Details/{_sharedReviewId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains(ReviewName, html);
        Assert.Contains(VisibleComment, html);
        Assert.DoesNotContain(InternalComment, html);
    }

    // ---- A2-06 ----

    [Fact]
    public async Task OrgBCustomer_OpeningOrgAReviewByUrl_IsAccessDeniedWithNoData()
    {
        var client = await factory.CreateSignedInClientAsync(_orgBCustomer, WebAppFactory.Password);

        var response = await client.GetAsync($"/SharedReviews/Details/{_sharedReviewId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.OriginalString);
        Assert.DoesNotContain(ReviewName, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OrgBCustomer_DoesNotSeeOrgAReviewInTheirList()
    {
        var client = await factory.CreateSignedInClientAsync(_orgBCustomer, WebAppFactory.Password);

        Assert.DoesNotContain(ReviewName, await client.GetStringAsync("/SharedReviews"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Customer_OpeningUnsharedOrMissingReview_IsAccessDenied(bool missing)
    {
        var client = await factory.CreateSignedInClientAsync(_orgACustomer, WebAppFactory.Password);

        var response = await client.GetAsync($"/SharedReviews/Details/{(missing ? 999_999 : _unsharedReviewId)}");

        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.OriginalString);
    }

    private async Task<int> AddOrganisationAsync(string name)
    {
        await factory.CreateAnonymousClientAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var organisation = new CustomerOrganisation { Name = name, ContactEmail = "contact@example.com" };
        db.CustomerOrganisations.Add(organisation);
        await db.SaveChangesAsync();
        return organisation.Id;
    }
}
