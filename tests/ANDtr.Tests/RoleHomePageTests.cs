using System.Net;
using ANDtr.Web.Data;
using ANDtr.Web.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ANDtr.Tests;

// Each role lands on a home page with content, not a blank page (A1-04).
public sealed class RoleHomePageTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task EmployeeHome_ShowsWelcomeAndFeatures()
    {
        var client = await factory.CreateSignedInClientAsync(Roles.Employee);

        var html = await client.GetStringAsync("/Search");

        Assert.Contains("Welcome back", html);
        Assert.Contains("Signed in as Employee", html);
        Assert.Contains("Search notices", html);
    }

    [Fact]
    public async Task CustomerHome_ShowsTheirOrganisationAndSharedReviewCount()
    {
        var organisationId = await AddOrganisationAsync("Home Page Clinic");
        var customer = await factory.CreateUserAsync("home-customer@test.local", Roles.Customer, organisationId);
        var client = await factory.CreateSignedInClientAsync(customer.Email!, WebAppFactory.Password);

        var response = await client.GetAsync("/SharedReviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Signed in as Customer", html);
        Assert.Contains("Home Page Clinic", html);
        Assert.Contains("Reviews shared with you", html);
    }

    [Theory]
    [InlineData(Roles.Employee, "/Reviews", "Your risk reviews")]
    [InlineData(Roles.Customer, "/Notifications", "New shared reviews")]
    public async Task OtherRolePages_ShowPlannedFeatures(string role, string path, string expected)
    {
        var client = await factory.CreateSignedInClientAsync(role);

        Assert.Contains(expected, await client.GetStringAsync(path));
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
