using ANDtr.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Data;

public sealed class DevDataSeeder(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    NoticeImporter noticeImporter,
    IConfiguration configuration,
    ILogger<DevDataSeeder> logger)
{
    private const string TestOrganisationName = "Sanjivani Healthcare";

    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                LogIfFailed(await roleManager.CreateAsync(new IdentityRole(role)), $"create role '{role}'");
            }
        }

        var organisation = await EnsureTestOrganisationAsync();

        await EnsureTestUserAsync(Roles.Employee, null);
        await EnsureTestUserAsync(Roles.Customer, organisation.Id);
        await EnsureTestUserAsync(Roles.Admin, null);

        await noticeImporter.ImportAsync();
    }

    private async Task<CustomerOrganisation> EnsureTestOrganisationAsync()
    {
        var organisation = await db.CustomerOrganisations.FirstOrDefaultAsync(o => o.Name == TestOrganisationName);
        if (organisation is null)
        {
            organisation = new CustomerOrganisation
            {
                Name = TestOrganisationName,
                ContactEmail = "contact@sanjivani.local",
                CreatedAt = DateTime.UtcNow,
            };
            db.CustomerOrganisations.Add(organisation);
            await db.SaveChangesAsync();
        }

        return organisation;
    }

    private async Task EnsureTestUserAsync(string role, int? organisationId)
    {
        var email = configuration[$"Seed:{role}:Email"];
        var password = configuration[$"Seed:{role}:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Skipping {Role} test user: set Seed:{Role}:Email and Seed:{Role}:Password with dotnet user-secrets (see SETUP.md).",
                role, role, role);
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = TestUserName(role),
                OrganisationId = organisationId,
                CreatedAt = DateTime.UtcNow,
            };

            if (!LogIfFailed(await userManager.CreateAsync(user, password), $"create {role} test user"))
            {
                return;
            }
        }
        else if (user.OrganisationId != organisationId || user.FullName == user.Email)
        {
            // Test users created before FullName existed were given their email as a name.
            user.OrganisationId = organisationId;
            user.FullName = user.FullName == user.Email ? TestUserName(role) : user.FullName;
            LogIfFailed(await userManager.UpdateAsync(user), $"update {role} test user");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            LogIfFailed(await userManager.AddToRoleAsync(user, role), $"add test user to role '{role}'");
        }
    }

    private static string TestUserName(string role) => role switch
    {
        Roles.Admin => "Vrunda Joshi",
        Roles.Employee => "Rahul Verma",
        _ => "Ananya Iyer",
    };

    private bool LogIfFailed(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            logger.LogWarning("Dev seeding could not {Action}: {Errors}", action,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return result.Succeeded;
    }
}
