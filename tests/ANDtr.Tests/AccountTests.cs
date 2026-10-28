using System.Net;
using ANDtr.Web.Data;

namespace ANDtr.Tests;

// Login, logout and deactivation through the real login page and cookie pipeline (A1, A3).
public sealed class AccountTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    [Fact]
    public async Task Login_WithCorrectPassword_SignsInAndRedirectsHome()
    {
        var client = await factory.CreateAnonymousClientAsync();

        var response = await factory.LoginAsync(client, "employee@test.local", WebAppFactory.Password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Search")).StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsErrorAndStaysSignedOut()
    {
        var client = await factory.CreateAnonymousClientAsync();

        var response = await factory.LoginAsync(client, "customer@test.local", "Wrong-Passw0rd");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid email or password.", await response.Content.ReadAsStringAsync());
        AssertRedirectsToLogin(await client.GetAsync("/SharedReviews"));
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShowsTheSameGeneralError()
    {
        var client = await factory.CreateAnonymousClientAsync();

        var response = await factory.LoginAsync(client, "nobody@test.local", WebAppFactory.Password);

        Assert.Contains("Invalid email or password.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logout_ThenOpenProtectedPage_AsksToLogInAgain()
    {
        var client = await factory.CreateSignedInClientAsync(Roles.Employee);

        var logout = await factory.PostFormAsync(client, "/Account/Logout", [], formPagePath: "/Search");

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        AssertRedirectsToLogin(await client.GetAsync("/Search"));
    }

    [Fact]
    public async Task Logout_TellsBrowserToClearItsPageCache()
    {
        var client = await factory.CreateSignedInClientAsync(Roles.Employee);

        var logout = await factory.PostFormAsync(client, "/Account/Logout", [], formPagePath: "/Search");

        Assert.Equal("\"cache\"", Assert.Single(logout.Headers.GetValues("Clear-Site-Data")));
    }

    [Fact]
    public async Task SignedInPage_IsNeverCached_SoBackButtonCannotShowIt()
    {
        var client = await factory.CreateSignedInClientAsync(Roles.Admin);

        var response = await client.GetAsync("/Admin/Users");

        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task RegisterPage_DoesNotExist()
    {
        var client = await factory.CreateAnonymousClientAsync();

        var response = await client.GetAsync("/Identity/Account/Register");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Root_WhenAnonymous_RedirectsToLogin()
    {
        var client = await factory.CreateAnonymousClientAsync();

        AssertRedirectsToLogin(await client.GetAsync("/"));
    }

    [Fact]
    public async Task AccessDeniedPage_ShowsMessageWithoutDetails()
    {
        var client = await factory.CreateSignedInClientAsync(Roles.Customer);

        var response = await client.GetAsync("/Account/AccessDenied");

        Assert.Contains("You do not have permission to view this page.", await response.Content.ReadAsStringAsync());
    }

    // ---- Deactivated users (A3-01, A3-02, A3-04) ----

    [Fact]
    public async Task Login_WhenDeactivated_IsRefusedWithClearMessage()
    {
        var user = await factory.CreateUserAsync("left-the-company@test.local", Roles.Employee);
        await factory.SetActiveAsync(user.Id, isActive: false);
        var client = await factory.CreateAnonymousClientAsync();

        var response = await factory.LoginAsync(client, user.Email!, WebAppFactory.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Your account is deactivated.", await response.Content.ReadAsStringAsync());
        AssertRedirectsToLogin(await client.GetAsync("/Search"));
    }

    [Fact]
    public async Task Login_WhenDeactivatedWithWrongPassword_ShowsOnlyTheGeneralError()
    {
        var user = await factory.CreateUserAsync("deactivated-wrong-password@test.local", Roles.Employee);
        await factory.SetActiveAsync(user.Id, isActive: false);
        var client = await factory.CreateAnonymousClientAsync();

        var response = await factory.LoginAsync(client, user.Email!, "Wrong-Passw0rd");

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password.", html);
        Assert.DoesNotContain("Your account is deactivated", html);
    }

    [Fact]
    public async Task NextRequest_AfterDeactivationWhileSignedIn_SignsTheUserOut()
    {
        var user = await factory.CreateUserAsync("signed-in-then-deactivated@test.local", Roles.Customer);
        var client = await factory.CreateSignedInClientAsync(user.Email!, WebAppFactory.Password);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/SharedReviews")).StatusCode);

        await factory.SetActiveAsync(user.Id, isActive: false);

        AssertRedirectsToLogin(await client.GetAsync("/SharedReviews"));
    }

    [Fact]
    public async Task Login_AfterReactivation_Succeeds()
    {
        var user = await factory.CreateUserAsync("reactivated@test.local", Roles.Employee);
        await factory.SetActiveAsync(user.Id, isActive: false);
        await factory.SetActiveAsync(user.Id, isActive: true);
        var client = await factory.CreateAnonymousClientAsync();

        var response = await factory.LoginAsync(client, user.Email!, WebAppFactory.Password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    // ---- Lockout and login tracking ----

    [Fact]
    public async Task Login_AfterFiveWrongPasswords_IsLockedUntilAdminUnlocks()
    {
        var user = await factory.CreateUserAsync("locked-out@test.local", Roles.Employee);
        var client = await factory.CreateAnonymousClientAsync();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await factory.LoginAsync(client, user.Email!, "Wrong-Passw0rd");
        }

        var locked = await factory.LoginAsync(client, user.Email!, WebAppFactory.Password);
        Assert.Contains("Too many failed attempts", await locked.Content.ReadAsStringAsync());

        var admin = await factory.CreateSignedInClientAsync(Roles.Admin);
        var unlock = await factory.PostFormAsync(admin, $"/Admin/Users/Unlock/{user.Id}", [], formPagePath: "/Admin/Users");
        Assert.Equal(HttpStatusCode.Redirect, unlock.StatusCode);

        var afterUnlock = await factory.LoginAsync(client, user.Email!, WebAppFactory.Password);
        Assert.Equal(HttpStatusCode.Redirect, afterUnlock.StatusCode);
    }

    [Fact]
    public async Task Login_Successful_RecordsLastLoginTime()
    {
        var user = await factory.CreateUserAsync("last-login@test.local", Roles.Employee);
        Assert.Null(await factory.GetLastLoginAsync(user.Id));

        await factory.CreateSignedInClientAsync(user.Email!, WebAppFactory.Password);

        Assert.NotNull(await factory.GetLastLoginAsync(user.Id));
    }

    private static void AssertRedirectsToLogin(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }
}
