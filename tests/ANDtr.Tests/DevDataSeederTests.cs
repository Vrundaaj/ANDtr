using ANDtr.Web.Data;
using ANDtr.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace ANDtr.Tests;

public sealed class DevDataSeederTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public DevDataSeederTests() => _connection.Open();

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task SeedAsync_WithoutSecrets_CreatesRolesAndSkipsUsers()
    {
        await using var provider = BuildServices(new Dictionary<string, string?>());

        await SeedAsync(provider);

        using var scope = provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(await roleManager.RoleExistsAsync(Roles.Employee));
        Assert.True(await roleManager.RoleExistsAsync(Roles.Customer));
        Assert.True(await roleManager.RoleExistsAsync(Roles.Admin));
        Assert.Empty(userManager.Users);
    }

    [Fact]
    public async Task SeedAsync_WithSecrets_CreatesConfirmedUserPerRole()
    {
        await using var provider = BuildServices(new Dictionary<string, string?>
        {
            ["Seed:Employee:Email"] = "employee@test.local",
            ["Seed:Employee:Password"] = "Test-Passw0rd!",
            ["Seed:Customer:Email"] = "customer@test.local",
            ["Seed:Customer:Password"] = "Test-Passw0rd!",
        });

        await SeedAsync(provider);

        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var employee = await userManager.FindByEmailAsync("employee@test.local");
        var customer = await userManager.FindByEmailAsync("customer@test.local");
        Assert.NotNull(employee);
        Assert.NotNull(customer);
        Assert.True(employee.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(employee, Roles.Employee));
        Assert.False(await userManager.IsInRoleAsync(employee, Roles.Customer));
        Assert.True(await userManager.IsInRoleAsync(customer, Roles.Customer));
        Assert.True(await userManager.CheckPasswordAsync(employee, "Test-Passw0rd!"));
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicate()
    {
        await using var provider = BuildServices(new Dictionary<string, string?>
        {
            ["Seed:Employee:Email"] = "employee@test.local",
            ["Seed:Employee:Password"] = "Test-Passw0rd!",
        });

        await SeedAsync(provider);
        await SeedAsync(provider);

        using var scope = provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Equal(3, roleManager.Roles.Count());
        Assert.Single(userManager.Users);
    }

    [Fact]
    public async Task SeedAsync_WithWeakPassword_LogsAndDoesNotThrow()
    {
        await using var provider = BuildServices(new Dictionary<string, string?>
        {
            ["Seed:Employee:Email"] = "employee@test.local",
            ["Seed:Employee:Password"] = "weak",
        });

        await SeedAsync(provider);

        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.Empty(userManager.Users);
    }

    private ServiceProvider BuildServices(Dictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = Environments.Development, ContentRootPath = AppContext.BaseDirectory });
        services.AddScoped<NoticeImporter>();
        services.AddScoped<DevDataSeeder>();
        return services.BuildServiceProvider();
    }

    private static async Task SeedAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DevDataSeeder>().SeedAsync();
    }
}
