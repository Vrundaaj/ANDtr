using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ANDtr.Tests;

// Unit tests for UserService (A1, A2, A3). Test IDs from the Users & Access test plan are noted.
public sealed class UserServiceTests : IAsyncLifetime
{
    private ServiceTestHost _host = null!;

    public async Task InitializeAsync() => _host = await ServiceTestHost.CreateAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    // ---- Create (A1-05 to A1-08, A2-03) ----

    [Fact]
    public async Task CreateAsync_WithValidDetails_CreatesActiveUserInRole()
    {
        var result = await CreateAsync(NewUser("rahul@andtr.com", Roles.Employee));

        Assert.True(result.Succeeded);
        var (user, roles) = await FindAsync("rahul@andtr.com");
        Assert.NotNull(user);
        Assert.True(user.IsActive);
        Assert.Equal("Rahul Verma", user.FullName);
        Assert.Equal([Roles.Employee], roles);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_IsRefused()
    {
        await CreateAsync(NewUser("rahul@andtr.com", Roles.Employee));

        var result = await CreateAsync(NewUser("RAHUL@andtr.com", Roles.Admin));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == nameof(UserFormViewModel.Email));
        Assert.Equal(1, await _host.RunAsync(sp => sp.GetRequiredService<ApplicationDbContext>().Users.CountAsync()));
    }

    [Fact]
    public async Task CreateAsync_WithWeakPassword_IsRefusedAndNotSaved()
    {
        var model = NewUser("rahul@andtr.com", Roles.Employee);
        model.Password = "123";

        var result = await CreateAsync(model);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == nameof(UserFormViewModel.Password));
        Assert.Null((await FindAsync("rahul@andtr.com")).User);
    }

    [Fact]
    public async Task CreateAsync_WithoutPassword_IsRefused()
    {
        var model = NewUser("rahul@andtr.com", Roles.Employee);
        model.Password = null;

        var result = await CreateAsync(model);

        Assert.Contains(result.Errors, e => e.Field == nameof(UserFormViewModel.Password));
    }

    [Fact]
    public async Task CreateAsync_CustomerWithoutOrganisation_IsRefused()
    {
        var result = await CreateAsync(NewUser("ananya@sanjivani.co.uk", Roles.Customer));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Field == nameof(UserFormViewModel.OrganisationId));
        Assert.Null((await FindAsync("ananya@sanjivani.co.uk")).User);
    }

    [Fact]
    public async Task CreateAsync_CustomerWithOrganisation_IsLinkedToIt()
    {
        var organisationId = await _host.AddOrganisationAsync("Sanjivani Ltd");
        var model = NewUser("ananya@sanjivani.co.uk", Roles.Customer);
        model.OrganisationId = organisationId;

        var result = await CreateAsync(model);

        Assert.True(result.Succeeded);
        Assert.Equal(organisationId, (await FindAsync("ananya@sanjivani.co.uk")).User!.OrganisationId);
    }

    [Fact]
    public async Task CreateAsync_EmployeeWithOrganisation_StoresNoOrganisation()
    {
        var model = NewUser("rahul@andtr.com", Roles.Employee);
        model.OrganisationId = await _host.AddOrganisationAsync("Sanjivani Ltd");

        await CreateAsync(model);

        Assert.Null((await FindAsync("rahul@andtr.com")).User!.OrganisationId);
    }

    // ---- Edit (A1-09) ----

    [Fact]
    public async Task UpdateAsync_ChangeEmployeeToAdmin_ReplacesRole()
    {
        var user = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var model = await EditModelAsync(user.Id);
        model.Role = Roles.Admin;

        var result = await UpdateAsync(model, currentUserId: "another-admin");

        Assert.True(result.Succeeded);
        Assert.Equal([Roles.Admin], (await FindAsync("rahul@andtr.com")).Roles);
    }

    [Fact]
    public async Task UpdateAsync_WithNewPassword_ChangesPassword()
    {
        var user = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var model = await EditModelAsync(user.Id);
        model.Password = "Changed-Passw0rd";

        var result = await UpdateAsync(model, currentUserId: "another-admin");

        Assert.True(result.Succeeded);
        Assert.True(await _host.RunAsync(async sp =>
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            return await userManager.CheckPasswordAsync((await userManager.FindByIdAsync(user.Id))!, "Changed-Passw0rd");
        }));
    }

    [Fact]
    public async Task UpdateAsync_WithEmailOfAnotherUser_IsRefused()
    {
        await _host.AddUserAsync("jo@andtr.com", Roles.Employee);
        var user = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var model = await EditModelAsync(user.Id);
        model.Email = "jo@andtr.com";

        var result = await UpdateAsync(model, currentUserId: "another-admin");

        Assert.Contains(result.Errors, e => e.Field == nameof(UserFormViewModel.Email));
    }

    [Fact]
    public async Task UpdateAsync_RemovingOwnAdminRole_IsRefused()
    {
        var admin = await _host.AddUserAsync("priya@andtr.com", Roles.Admin);
        var model = await EditModelAsync(admin.Id);
        model.Role = Roles.Employee;

        var result = await UpdateAsync(model, currentUserId: admin.Id);

        Assert.False(result.Succeeded);
        Assert.Equal([Roles.Admin], (await FindAsync("priya@andtr.com")).Roles);
    }

    // ---- Deactivate / reactivate (A3) ----

    [Fact]
    public async Task SetActiveAsync_Deactivate_SetsIsActiveFalse()
    {
        var user = await _host.AddUserAsync("sneha@arogya.com", Roles.Employee);

        var result = await SetActiveAsync(user.Id, isActive: false);

        Assert.True(result.Succeeded);
        Assert.False((await FindAsync("sneha@arogya.com")).User!.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_Deactivate_KeepsTheUsersComments()
    {
        var user = await _host.AddUserAsync("sneha@andtr.com", Roles.Employee);
        var reviewId = await _host.AddReviewAsync(user.Id, sharedWithOrganisationId: null);
        await _host.RunAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.Comments.Add(new Comment { RiskReviewId = reviewId, AuthorId = user.Id, Body = "Check lot numbers", CreatedAt = DateTime.UtcNow });
            return await db.SaveChangesAsync();
        });

        await SetActiveAsync(user.Id, isActive: false);

        var comments = await _host.RunAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Comments.Where(c => c.AuthorId == user.Id).ToListAsync());
        Assert.Single(comments);
        Assert.NotNull((await FindAsync("sneha@andtr.com")).User);
    }

    [Fact]
    public async Task SetActiveAsync_Reactivate_SetsIsActiveTrue()
    {
        var user = await _host.AddUserAsync("sneha@arogya.com", Roles.Employee);
        await SetActiveAsync(user.Id, isActive: false);

        var result = await SetActiveAsync(user.Id, isActive: true);

        Assert.True(result.Succeeded);
        Assert.True((await FindAsync("sneha@arogya.com")).User!.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_OwnAccount_IsRefused()
    {
        var admin = await _host.AddUserAsync("priya@andtr.com", Roles.Admin);

        var result = await _host.RunAsync(sp =>
            sp.GetRequiredService<UserService>().SetActiveAsync(admin.Id, isActive: false, currentUserId: admin.Id));

        Assert.False(result.Succeeded);
        Assert.True((await FindAsync("priya@andtr.com")).User!.IsActive);
    }

    // ---- Delete and restore ----

    [Fact]
    public async Task DeleteAsync_UserWithReview_MovesToDeletedListAndKeepsReview()
    {
        var user = await _host.AddUserAsync("sneha@andtr.com", Roles.Employee);
        await _host.AddReviewAsync(user.Id, sharedWithOrganisationId: null);

        var result = await DeleteAsync(user.Id, currentUserId: "another-admin");

        Assert.True(result.Succeeded);
        var deleted = (await FindAsync("sneha@andtr.com")).User!;
        Assert.NotNull(deleted.DeletedAt);
        Assert.False(deleted.IsActive);
        Assert.Equal(1, await _host.RunAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().RiskReviews.CountAsync(r => r.OwnerId == user.Id)));
    }

    [Fact]
    public async Task SearchAsync_DeletedUser_OnlyShownInDeletedList()
    {
        var deleted = await _host.AddUserAsync("sneha@andtr.com", Roles.Employee);
        await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        await DeleteAsync(deleted.Id, currentUserId: "another-admin");

        var main = await SearchAsync(new UserListViewModel());
        var deletedList = await SearchAsync(new UserListViewModel { Status = "deleted" });

        Assert.Equal(["rahul@andtr.com"], main.Users.Select(u => u.Email));
        Assert.Equal(["sneha@andtr.com"], deletedList.Users.Select(u => u.Email));
    }

    [Fact]
    public async Task RestoreAsync_DeletedUser_IsActiveAndListedAgain()
    {
        var user = await _host.AddUserAsync("sneha@andtr.com", Roles.Employee);
        await DeleteAsync(user.Id, currentUserId: "another-admin");

        var result = await _host.RunAsync(sp => sp.GetRequiredService<UserService>().RestoreAsync(user.Id));

        Assert.True(result.Succeeded);
        var restored = (await FindAsync("sneha@andtr.com")).User!;
        Assert.Null(restored.DeletedAt);
        Assert.True(restored.IsActive);
    }

    [Fact]
    public async Task SetActiveAsync_DeletedUser_IsRefused()
    {
        var user = await _host.AddUserAsync("sneha@andtr.com", Roles.Employee);
        await DeleteAsync(user.Id, currentUserId: "another-admin");

        var result = await SetActiveAsync(user.Id, isActive: true);

        Assert.False(result.Succeeded);
        Assert.False((await FindAsync("sneha@andtr.com")).User!.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_OwnAccount_IsRefused()
    {
        var admin = await _host.AddUserAsync("priya@andtr.com", Roles.Admin);

        var result = await DeleteAsync(admin.Id, currentUserId: admin.Id);

        Assert.False(result.Succeeded);
        Assert.NotNull((await FindAsync("priya@andtr.com")).User);
    }

    // ---- Users list ----

    [Fact]
    public async Task SearchAsync_WithRoleAndStatusFilters_ReturnsOnlyMatchingUsers()
    {
        await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var inactive = await _host.AddUserAsync("jo@andtr.com", Roles.Employee);
        await _host.AddUserAsync("priya@andtr.com", Roles.Admin);
        await SetActiveAsync(inactive.Id, isActive: false);

        var page = await _host.RunAsync(sp => sp.GetRequiredService<UserService>()
            .SearchAsync(new UserListViewModel { Role = Roles.Employee, IsActive = true }));

        var user = Assert.Single(page.Users);
        Assert.Equal("rahul@andtr.com", user.Email);
        Assert.Equal(Roles.Employee, user.Role);
    }

    [Fact]
    public async Task SearchAsync_SortByEmailDescending_OrdersUsersByEmail()
    {
        await _host.AddUserAsync("ananya@andtr.com", Roles.Employee);
        await _host.AddUserAsync("zoya@andtr.com", Roles.Employee);
        await _host.AddUserAsync("mohan@andtr.com", Roles.Employee);

        var page = await _host.RunAsync(sp => sp.GetRequiredService<UserService>()
            .SearchAsync(new UserListViewModel { Sort = UserSort.Email, Desc = true }));

        Assert.Equal(["zoya@andtr.com", "mohan@andtr.com", "ananya@andtr.com"], page.Users.Select(u => u.Email));
    }

    [Fact]
    public async Task SearchAsync_WithMoreUsersThanAPage_ReturnsOnePageButCountsAll()
    {
        for (var i = 0; i < UserService.PageSize + 2; i++)
        {
            await _host.AddUserAsync($"user{i:00}@andtr.com", Roles.Employee);
        }

        var page = await _host.RunAsync(sp => sp.GetRequiredService<UserService>()
            .SearchAsync(new UserListViewModel { Page = 2 }));

        Assert.Equal(UserService.PageSize + 2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(2, page.Users.Count);
    }

    // ---- Unlock and login tracking ----

    [Fact]
    public async Task UnlockAsync_LockedOutUser_ClearsTheLockout()
    {
        var user = await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        await _host.RunAsync(async sp =>
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            return await userManager.SetLockoutEndDateAsync((await userManager.FindByIdAsync(user.Id))!, DateTimeOffset.UtcNow.AddMinutes(5));
        });

        var result = await _host.RunAsync(sp => sp.GetRequiredService<UserService>().UnlockAsync(user.Id));

        Assert.True(result.Succeeded);
        var page = await _host.RunAsync(sp => sp.GetRequiredService<UserService>().SearchAsync(new UserListViewModel()));
        Assert.False(Assert.Single(page.Users).IsLocked);
    }

    [Fact]
    public async Task RecordLoginAsync_SetsLastLoginTime()
    {
        await _host.AddUserAsync("rahul@andtr.com", Roles.Employee);
        var before = DateTime.UtcNow;

        await _host.RunAsync(async sp =>
        {
            await sp.GetRequiredService<UserService>().RecordLoginAsync("rahul@andtr.com");
            return true;
        });

        var lastLogin = (await FindAsync("rahul@andtr.com")).User!.LastLoginAt;
        Assert.NotNull(lastLogin);
        Assert.InRange(lastLogin.Value, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    // ---- Helpers ----

    private static UserFormViewModel NewUser(string email, string role) => new()
    {
        FullName = "Rahul Verma",
        Email = email,
        Password = "Strong-Passw0rd",
        ConfirmPassword = "Strong-Passw0rd",
        Role = role,
    };

    private Task<ServiceResult> CreateAsync(UserFormViewModel model) =>
        _host.RunAsync(sp => sp.GetRequiredService<UserService>().CreateAsync(model));

    private Task<ServiceResult> UpdateAsync(UserFormViewModel model, string currentUserId) =>
        _host.RunAsync(sp => sp.GetRequiredService<UserService>().UpdateAsync(model, currentUserId));

    private Task<ServiceResult> SetActiveAsync(string id, bool isActive) =>
        _host.RunAsync(sp => sp.GetRequiredService<UserService>().SetActiveAsync(id, isActive, currentUserId: "another-admin"));

    private Task<ServiceResult> DeleteAsync(string id, string currentUserId) =>
        _host.RunAsync(sp => sp.GetRequiredService<UserService>().DeleteAsync(id, currentUserId));

    private Task<UserListViewModel> SearchAsync(UserListViewModel filter) =>
        _host.RunAsync(sp => sp.GetRequiredService<UserService>().SearchAsync(filter));

    private async Task<UserFormViewModel> EditModelAsync(string id) =>
        (await _host.RunAsync(sp => sp.GetRequiredService<UserService>().GetForEditAsync(id)))!;

    private Task<(ApplicationUser? User, IList<string> Roles)> FindAsync(string email) =>
        _host.RunAsync<(ApplicationUser?, IList<string>)>(async sp =>
        {
            var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            return (user, user is null ? [] : await userManager.GetRolesAsync(user));
        });
}
