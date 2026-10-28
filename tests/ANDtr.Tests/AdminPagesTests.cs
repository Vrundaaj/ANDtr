using System.Net;
using ANDtr.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ANDtr.Tests;

// The admin Users and Organisations pages end to end, posting the real forms as an admin (A1, A2, A3).
public sealed class AdminPagesTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    // ---- Users (A1-05, A1-06, A1-08, A2-03, A3) ----

    [Fact]
    public async Task CreateUser_WithValidDetails_SavesUserWhoCanLogIn()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var response = await PostUserAsync(admin, "new-employee@test.local", Roles.Employee);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var list = await admin.GetStringAsync("/Admin/Users?search=new-employee");
        Assert.Contains("new-employee@test.local", list);
        await factory.CreateSignedInClientAsync("new-employee@test.local", WebAppFactory.Password);
    }

    [Fact]
    public async Task UsersList_SearchWithNoMatch_ShowsNoSearchResultsFound()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var list = await admin.GetStringAsync("/Admin/Users?search=zzz-nobody");

        Assert.Contains("No search results found", list);
        Assert.Contains("id=\"user-results\"", list);
    }

    [Fact]
    public async Task UsersList_SearchPartOfName_ListsEveryMatchingUser()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);
        await PostUserAsync(admin, "search-one@test.local", Roles.Employee);
        await PostUserAsync(admin, "search-two@test.local", Roles.Employee);

        var list = await admin.GetStringAsync("/Admin/Users?search=SEARCH-");

        Assert.Contains("search-one@test.local", list);
        Assert.Contains("search-two@test.local", list);
        Assert.DoesNotContain("No search results found", list);
    }

    [Fact]
    public async Task CreateUser_WithEmailAlreadyUsed_ShowsErrorAndDoesNotCreate()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var response = await PostUserAsync(admin, "employee@test.local", Roles.Employee);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("This email is already used by another account.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ShowsErrorAndDoesNotCreate()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var response = await PostUserAsync(admin, "ananya@sanjivani", Roles.Employee);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Please enter a valid email address.", await response.Content.ReadAsStringAsync());
        Assert.Null(await FindUserAsync("ananya@sanjivani"));
    }

    [Fact]
    public async Task CreateUser_CustomerWithNoOrganisation_ShowsErrorAndDoesNotCreate()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var response = await PostUserAsync(admin, "no-org-customer@test.local", Roles.Customer);

        Assert.Contains("Pick an organisation for this customer.", await response.Content.ReadAsStringAsync());
        Assert.Null(await FindUserAsync("no-org-customer@test.local"));
    }

    [Fact]
    public async Task Deactivate_FromUsersList_MarksInactiveAndShowsMessage()
    {
        var user = await factory.CreateUserAsync("to-deactivate@test.local", Roles.Employee);
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var response = await factory.PostFormAsync(admin, $"/Admin/Users/Deactivate/{user.Id}", [], formPagePath: "/Admin/Users");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False((await FindUserAsync("to-deactivate@test.local"))!.IsActive);
        Assert.Contains("was deactivated", await admin.GetStringAsync("/Admin/Users"));
    }

    [Fact]
    public async Task UsersPage_ShowsOnlyTheUsersList()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var html = await admin.GetStringAsync("/Admin/Users");

        Assert.Contains("user-results", html);
        Assert.DoesNotContain("Total users", html);
        Assert.DoesNotContain("Users by role", html);
        Assert.DoesNotContain("Customers per organisation", html);
    }

    // ---- Organisations (A2-01, A2-02, A2-04) ----

    [Fact]
    public async Task CreateOrganisation_ShowsInList()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);

        var response = await PostOrganisationAsync(admin, "Sanjivani Ltd");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Sanjivani Ltd", await admin.GetStringAsync("/Admin/Organisations"));
    }

    [Fact]
    public async Task CreateCustomer_LinkedToOrganisation_ShowsUnderThatOrganisation()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);
        await PostOrganisationAsync(admin, "NHS Trust East");
        var organisationId = await OrganisationIdAsync("NHS Trust East");

        await PostUserAsync(admin, "linked-customer@test.local", Roles.Customer, organisationId);

        Assert.Contains("linked-customer@test.local", await admin.GetStringAsync($"/Admin/Organisations/Edit/{organisationId}"));
    }

    [Fact]
    public async Task DeleteOrganisation_WithUsers_IsBlockedWithMessage()
    {
        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);
        await PostOrganisationAsync(admin, "Arogya");
        var organisationId = await OrganisationIdAsync("Arogya");
        await factory.CreateUserAsync("arogya-customer@test.local", Roles.Customer, organisationId);

        var response = await factory.PostFormAsync(admin, $"/Admin/Organisations/Delete/{organisationId}", [], formPagePath: "/Admin/Organisations");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("cannot be deleted because it still has 1 user", await admin.GetStringAsync("/Admin/Organisations"));
        Assert.Equal(organisationId, await OrganisationIdAsync("Arogya"));
    }

    // ---- Helpers ----

    private Task<HttpResponseMessage> PostUserAsync(HttpClient admin, string email, string role, int? organisationId = null) =>
        factory.PostFormAsync(admin, "/Admin/Users/Create", new Dictionary<string, string>
        {
            ["FullName"] = "Test Person",
            ["Email"] = email,
            ["Password"] = WebAppFactory.Password,
            ["ConfirmPassword"] = WebAppFactory.Password,
            ["Role"] = role,
            ["OrganisationId"] = organisationId?.ToString() ?? string.Empty,
            ["IsActive"] = "true",
        });

    private Task<HttpResponseMessage> PostOrganisationAsync(HttpClient admin, string name) =>
        factory.PostFormAsync(admin, "/Admin/Organisations/Create", new Dictionary<string, string>
        {
            ["Name"] = name,
            ["ContactEmail"] = "contact@example.com",
        });

    private async Task<Web.Models.ApplicationUser?> FindUserAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email);
    }

    private async Task<int> OrganisationIdAsync(string name)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .CustomerOrganisations.Where(o => o.Name == name).Select(o => o.Id).SingleAsync();
    }
}
