using System.Security.Claims;
using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ANDtr.Tests;

// Unit tests for OrganisationService: organisations (A2) and the customer privacy helpers.
public sealed class OrganisationServiceTests : IAsyncLifetime
{
    private ServiceTestHost _host = null!;

    public async Task InitializeAsync() => _host = await ServiceTestHost.CreateAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    // ---- Create, edit and delete (A2-01, A2-04) ----

    [Fact]
    public async Task CreateAsync_WithValidDetails_AppearsInList()
    {
        var result = await RunAsync(s => s.CreateAsync(NewOrganisation("Sanjivani Ltd")));

        Assert.True(result.Succeeded);
        var organisation = Assert.Single(await RunAsync(s => s.ListAsync(search: null)));
        Assert.Equal("Sanjivani Ltd", organisation.Name);
        Assert.Equal("contact@sanjivani.co.uk", organisation.ContactEmail);
    }

    [Fact]
    public async Task CreateAsync_WithNameAlreadyUsedInAnotherCase_IsRefused()
    {
        await RunAsync(s => s.CreateAsync(NewOrganisation("Sanjivani Ltd")));

        var result = await RunAsync(s => s.CreateAsync(NewOrganisation("sanjivani ltd")));

        Assert.Contains(result.Errors, e => e.Field == nameof(OrganisationFormViewModel.Name));
        Assert.Single(await RunAsync(s => s.ListAsync(search: null)));
    }

    [Fact]
    public async Task UpdateAsync_WithNewDetails_SavesThem()
    {
        var id = await _host.AddOrganisationAsync("Arogya");
        var model = NewOrganisation("Arogya Group");
        model.Id = id;

        var result = await RunAsync(s => s.UpdateAsync(model));

        Assert.True(result.Succeeded);
        Assert.Equal("Arogya Group", (await RunAsync(s => s.GetForEditAsync(id)))!.Name);
    }

    [Fact]
    public async Task DeleteAsync_WithUsers_IsBlockedWithMessage()
    {
        var id = await _host.AddOrganisationAsync("Arogya");
        await _host.AddUserAsync("sneha@arogya.com", Roles.Customer, id);
        await _host.AddUserAsync("ravi@arogya.com", Roles.Customer, id);

        var result = await RunAsync(s => s.DeleteAsync(id));

        Assert.False(result.Succeeded);
        Assert.Contains("still has 2 users", result.Errors[0].Message);
        Assert.NotNull(await RunAsync(s => s.GetForEditAsync(id)));
    }

    [Fact]
    public async Task DeleteAsync_WithSharedReviews_IsBlocked()
    {
        var id = await _host.AddOrganisationAsync("Arogya");
        var employee = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        await _host.AddReviewAsync(employee.Id, id);

        var result = await RunAsync(s => s.DeleteAsync(id));

        Assert.False(result.Succeeded);
        Assert.Contains("shared review", result.Errors[0].Message);
    }

    [Fact]
    public async Task DeleteAsync_WithNoUsersOrReviews_Deletes()
    {
        var id = await _host.AddOrganisationAsync("CarePoint Clinics");

        var result = await RunAsync(s => s.DeleteAsync(id));

        Assert.True(result.Succeeded);
        Assert.Null(await RunAsync(s => s.GetForEditAsync(id)));
    }

    // ---- Customer privacy (A2-05, A2-06) ----

    [Fact]
    public async Task SharedWith_ReturnsOnlyThatOrganisationsSharedReviews()
    {
        var orgA = await _host.AddOrganisationAsync("Org A");
        var orgB = await _host.AddOrganisationAsync("Org B");
        var employee = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var sharedWithA = await _host.AddReviewAsync(employee.Id, orgA);
        await _host.AddReviewAsync(employee.Id, orgB);
        await _host.AddReviewAsync(employee.Id, sharedWithOrganisationId: null);

        var visible = await RunAsync(s => s.SharedWith(orgA).Select(r => r.Id).ToListAsync());

        Assert.Equal([sharedWithA], visible);
    }

    [Fact]
    public async Task CanViewReviewAsync_ReviewSharedWithOwnOrganisation_ReturnsTrue()
    {
        var orgA = await _host.AddOrganisationAsync("Org A");
        var customer = await _host.AddUserAsync("ananya@orga.com", Roles.Customer, orgA);
        var employee = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var reviewId = await _host.AddReviewAsync(employee.Id, orgA);

        Assert.True(await RunAsync(s => s.CanViewReviewAsync(PrincipalFor(customer), reviewId)));
    }

    [Fact]
    public async Task CanViewReviewAsync_ReviewSharedWithAnotherOrganisation_ReturnsFalse()
    {
        var orgA = await _host.AddOrganisationAsync("Org A");
        var orgB = await _host.AddOrganisationAsync("Org B");
        var orgBCustomer = await _host.AddUserAsync("arjun@orgb.com", Roles.Customer, orgB);
        var employee = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var orgAReview = await _host.AddReviewAsync(employee.Id, orgA);

        Assert.False(await RunAsync(s => s.CanViewReviewAsync(PrincipalFor(orgBCustomer), orgAReview)));
    }

    [Fact]
    public async Task CanViewReviewAsync_UserWithoutOrganisation_ReturnsFalse()
    {
        var orgA = await _host.AddOrganisationAsync("Org A");
        var employee = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var reviewId = await _host.AddReviewAsync(employee.Id, orgA);

        Assert.False(await RunAsync(s => s.CanViewReviewAsync(PrincipalFor(employee), reviewId)));
    }

    [Fact]
    public async Task GetOrganisationIdAsync_ForCustomer_ReturnsTheirOrganisation()
    {
        var orgA = await _host.AddOrganisationAsync("Org A");
        var customer = await _host.AddUserAsync("ananya@orga.com", Roles.Customer, orgA);

        Assert.Equal(orgA, await RunAsync(s => s.GetOrganisationIdAsync(PrincipalFor(customer))));
    }

    // ---- Helpers ----

    private static OrganisationFormViewModel NewOrganisation(string name) => new()
    {
        Name = name,
        ContactEmail = "contact@sanjivani.co.uk",
    };

    private static ClaimsPrincipal PrincipalFor(ApplicationUser user) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "Test"));

    private Task<T> RunAsync<T>(Func<OrganisationService, Task<T>> action) =>
        _host.RunAsync(sp => action(sp.GetRequiredService<OrganisationService>()));
}
