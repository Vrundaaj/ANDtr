using System.Net;
using System.Text.RegularExpressions;
using ANDtr.Web.Data;
using ANDtr.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ANDtr.Tests;

public sealed partial class WebAppFactory : WebApplicationFactory<Program>
{
    public const string Password = "Test-Passw0rd!";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Task? _seeding;

    public WebAppFactory() => _connection.Open();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();

    public async Task<HttpClient> CreateAnonymousClientAsync()
    {
        await EnsureSeededAsync();
        return CreateDefaultClient();
    }

    public Task<HttpClient> CreateSignedInClientAsync(string role) =>
        CreateSignedInClientAsync($"{role.ToLowerInvariant()}@test.local", Password);

    public async Task<HttpClient> CreateSignedInClientAsync(string email, string password)
    {
        var client = await CreateAnonymousClientAsync();

        var response = await LoginAsync(client, email, password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    public Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        PostFormAsync(client, "/Account/Login", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
        });

    // Loads the form page for its antiforgery token, then posts the fields to the same path.
    public async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string path, Dictionary<string, string> fields, string? formPagePath = null)
    {
        var formPage = await client.GetAsync(formPagePath ?? path);
        formPage.EnsureSuccessStatusCode();
        fields["__RequestVerificationToken"] =
            AntiforgeryToken().Match(await formPage.Content.ReadAsStringAsync()).Groups[1].Value;

        return await client.PostAsync(path, new FormUrlEncodedContent(fields));
    }

    // Adds an extra user for tests that change account state, so the shared role users stay intact.
    public async Task<ApplicationUser> CreateUserAsync(string email, string role, int? organisationId = null)
    {
        await EnsureSeededAsync();

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = email,
            OrganisationId = organisationId,
        };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    public async Task SetActiveAsync(string userId, bool isActive)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await db.Users.SingleAsync(u => u.Id == userId);
        user.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<DateTime?> GetLastLoginAsync(string userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.LastLoginAt).SingleAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        });
    }

    private HttpClient CreateDefaultClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private async Task EnsureSeededAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _seeding ??= SeedAsync();
        }
        finally
        {
            _gate.Release();
        }

        await _seeding;
    }

    private async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles.All)
        {
            await roleManager.CreateAsync(new IdentityRole(role));

            var email = $"{role.ToLowerInvariant()}@test.local";
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            await userManager.CreateAsync(user, Password);
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
