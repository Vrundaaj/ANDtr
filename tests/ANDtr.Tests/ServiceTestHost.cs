using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ANDtr.Tests;

// In-memory SQLite database with Identity and the Users & Access services, for service-level tests.
public sealed class ServiceTestHost : IAsyncDisposable
{
    public const string Password = "Test-Passw0rd!";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ServiceProvider _provider;

    private ServiceTestHost()
    {
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>(IdentitySetup.ConfigureOptions)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<UserService>();
        services.AddScoped<OrganisationService>();
        _provider = services.BuildServiceProvider();
    }

    public static async Task<ServiceTestHost> CreateAsync()
    {
        var host = new ServiceTestHost();

        await using var scope = host.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        return host;
    }

    // Use a fresh scope per step, so each step reads from the database rather than a cached entity.
    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public async Task<T> RunAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = CreateScope();
        return await action(scope.ServiceProvider);
    }

    public async Task<int> AddOrganisationAsync(string name)
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var organisation = new CustomerOrganisation { Name = name, ContactEmail = "contact@example.com" };
        db.CustomerOrganisations.Add(organisation);
        await db.SaveChangesAsync();
        return organisation.Id;
    }

    public async Task<ApplicationUser> AddUserAsync(string email, string role, int? organisationId = null)
    {
        await using var scope = CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = email,
            OrganisationId = organisationId,
        };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    public async Task<int> AddReviewAsync(string ownerId, int? sharedWithOrganisationId)
    {
        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var review = new RiskReview
        {
            Name = "Infusion pump review",
            OwnerId = ownerId,
            OrganisationId = sharedWithOrganisationId,
            CreatedAt = DateTime.UtcNow,
            SharedAt = sharedWithOrganisationId is null ? null : DateTime.UtcNow,
        };
        db.RiskReviews.Add(review);
        await db.SaveChangesAsync();
        return review.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
