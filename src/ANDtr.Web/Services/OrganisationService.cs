using System.Security.Claims;
using ANDtr.Web.Data;
using ANDtr.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Services;

// Customer organisations (A2) and the customer privacy helpers used by the customer portal.
public sealed class OrganisationService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<OrganisationService> logger)
{
    private const string SaveFailedMessage = "The changes could not be saved. Please try again.";

    // ---- Organisations list ----

    public Task<List<OrganisationListItem>> ListAsync(string? search)
    {
        var query = db.CustomerOrganisations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => EF.Functions.Like(o.Name, $"%{search.Trim()}%"));
        }

        return query
            .OrderBy(o => o.Name)
            .Select(o => new OrganisationListItem
            {
                Id = o.Id,
                Name = o.Name,
                ContactEmail = o.ContactEmail,
                UserCount = o.Users.Count,
                SharedReviewCount = o.RiskReviews.Count(r => r.SharedAt != null),
                CreatedAt = o.CreatedAt,
            })
            .ToListAsync();
    }

    // Dropdown options for the user form.
    public Task<List<SelectListItem>> GetOptionsAsync() =>
        db.CustomerOrganisations.AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new SelectListItem(o.Name, o.Id.ToString()))
            .ToListAsync();

    public Task<OrganisationFormViewModel?> GetForEditAsync(int id) =>
        db.CustomerOrganisations.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrganisationFormViewModel
            {
                Id = o.Id,
                Name = o.Name,
                ContactEmail = o.ContactEmail,
                Notes = o.Notes,
            })
            .FirstOrDefaultAsync();

    // ---- Create, edit and delete ----

    public async Task<ServiceResult> CreateAsync(OrganisationFormViewModel model)
    {
        if (await NameTakenAsync(model.Name, exceptId: null))
        {
            return NameTakenError();
        }

        db.CustomerOrganisations.Add(new CustomerOrganisation
        {
            Name = model.Name.Trim(),
            ContactEmail = model.ContactEmail.Trim(),
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            CreatedAt = DateTime.UtcNow,
        });

        return await SaveAsync();
    }

    public async Task<ServiceResult> UpdateAsync(OrganisationFormViewModel model)
    {
        var organisation = await db.CustomerOrganisations.FindAsync(model.Id);
        if (organisation is null)
        {
            return ServiceResult.Fail(string.Empty, "This organisation no longer exists.");
        }

        if (await NameTakenAsync(model.Name, organisation.Id))
        {
            return NameTakenError();
        }

        organisation.Name = model.Name.Trim();
        organisation.ContactEmail = model.ContactEmail.Trim();
        organisation.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        return await SaveAsync();
    }

    // Blocked while users or reviews still point at the organisation, so no customer is left
    // without one and no shared review loses its target.
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var organisation = await db.CustomerOrganisations
            .Where(o => o.Id == id)
            .Select(o => new { Entity = o, UserCount = o.Users.Count, ReviewCount = o.RiskReviews.Count })
            .FirstOrDefaultAsync();

        if (organisation is null)
        {
            return ServiceResult.Fail(string.Empty, "This organisation no longer exists.");
        }

        if (organisation.UserCount > 0)
        {
            return ServiceResult.Fail(string.Empty,
                $"“{organisation.Entity.Name}” cannot be deleted because it still has {Plural(organisation.UserCount, "user")}.");
        }

        if (organisation.ReviewCount > 0)
        {
            return ServiceResult.Fail(string.Empty,
                $"“{organisation.Entity.Name}” cannot be deleted because it still has {Plural(organisation.ReviewCount, "shared review")}.");
        }

        db.CustomerOrganisations.Remove(organisation.Entity);
        return await SaveAsync();
    }

    // ---- Customer privacy (for the customer portal) ----

    // The signed-in customer's organisation, or null for staff and unknown users.
    public async Task<int?> GetOrganisationIdAsync(ClaimsPrincipal principal)
    {
        var userId = userManager.GetUserId(principal);
        if (userId is null)
        {
            return null;
        }

        return await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.OrganisationId)
            .FirstOrDefaultAsync();
    }

    // The only reviews a customer in this organisation may see: those shared with it.
    public IQueryable<RiskReview> SharedWith(int organisationId) =>
        db.RiskReviews.Where(r => r.OrganisationId == organisationId && r.SharedAt != null);

    // Call before showing a review to a customer; false means show Access denied.
    public async Task<bool> CanViewReviewAsync(ClaimsPrincipal principal, int reviewId)
    {
        var organisationId = await GetOrganisationIdAsync(principal);
        return organisationId is int id && await SharedWith(id).AnyAsync(r => r.Id == reviewId);
    }

    // ---- Helpers ----

    private Task<bool> NameTakenAsync(string name, int? exceptId)
    {
        var normalised = name.Trim().ToLower();
        return db.CustomerOrganisations.AnyAsync(o => o.Id != exceptId && o.Name.ToLower() == normalised);
    }

    private static ServiceResult NameTakenError() =>
        ServiceResult.Fail(nameof(OrganisationFormViewModel.Name), "An organisation with this name already exists.");

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";

    private async Task<ServiceResult> SaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
            return ServiceResult.Success;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Saving an organisation failed");
            return ServiceResult.Fail(string.Empty, SaveFailedMessage);
        }
    }
}
