using System.Linq.Expressions;
using ANDtr.Web.Data;
using ANDtr.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ANDtr.Web.Services;

// Create, edit, list and deactivate user accounts (A1, A2, A3).
public sealed class UserService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<UserService> logger)
{
    public const int PageSize = 10;

    private const string SaveFailedMessage = "The changes could not be saved. Please try again.";

    private const string DeletedMessage = "This user has been deleted. Restore them from the Deleted list first.";

    // ---- Users list ----

    // Fills in one page of users matching the filters and sort already set on the model.
    public async Task<UserListViewModel> SearchAsync(UserListViewModel filter)
    {
        var query = await FilterAsync(filter);

        filter.PageSize = PageSize;
        filter.TotalCount = await query.CountAsync();
        filter.Page = Math.Clamp(filter.Page, 1, filter.TotalPages);
        filter.Users = await ToListItems(Sort(query, filter.Sort, filter.Desc)
                .Skip((filter.Page - 1) * PageSize)
                .Take(PageSize))
            .ToListAsync();

        return filter;
    }

    // Customer users in one organisation, for the Edit organisation page.
    public Task<List<UserListItem>> GetOrganisationMembersAsync(int organisationId) =>
        ToListItems(db.Users.AsNoTracking()
                .Where(u => u.OrganisationId == organisationId && u.DeletedAt == null)
                .OrderBy(u => u.FullName))
            .ToListAsync();

    public async Task<UserFormViewModel?> GetForEditAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? Roles.Employee,
            OrganisationId = user.OrganisationId,
            IsActive = user.IsActive,
        };
    }

    // ---- Create and edit ----

    public async Task<ServiceResult> CreateAsync(UserFormViewModel model)
    {
        var errors = await ValidateAsync(model, existingUserId: null);
        if (string.IsNullOrEmpty(model.Password))
        {
            errors.Add(new ServiceError(nameof(model.Password), "Enter a password."));
        }

        if (errors.Count > 0)
        {
            return new ServiceResult(errors);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            OrganisationId = OrganisationFor(model),
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        return await SaveInTransactionAsync(async () =>
        {
            var result = await userManager.CreateAsync(user, model.Password!);
            if (result.Succeeded)
            {
                result = await userManager.AddToRoleAsync(user, model.Role);
            }

            return FromIdentity(result);
        });
    }

    // currentUserId is the admin making the change; admins cannot lock themselves out.
    public async Task<ServiceResult> UpdateAsync(UserFormViewModel model, string currentUserId)
    {
        var user = model.Id is null ? null : await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return ServiceResult.Fail(string.Empty, "This user no longer exists.");
        }

        if (user.DeletedAt is not null)
        {
            return ServiceResult.Fail(string.Empty, DeletedMessage);
        }

        if (user.Id == currentUserId && (model.Role != Roles.Admin || !model.IsActive))
        {
            return ServiceResult.Fail(string.Empty, "You cannot remove your own admin role or deactivate your own account.");
        }

        var errors = await ValidateAsync(model, user.Id);
        if (errors.Count > 0)
        {
            return new ServiceResult(errors);
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.OrganisationId = OrganisationFor(model);
        user.IsActive = model.IsActive;

        return await SaveInTransactionAsync(async () =>
        {
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return FromIdentity(result);
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            if (!currentRoles.SequenceEqual([model.Role]))
            {
                result = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (result.Succeeded)
                {
                    result = await userManager.AddToRoleAsync(user, model.Role);
                }

                if (!result.Succeeded)
                {
                    return FromIdentity(result);
                }
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                // A reset token lets the admin set a new password without knowing the old one.
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                result = await userManager.ResetPasswordAsync(user, token, model.Password);
            }

            return FromIdentity(result);
        });
    }

    // ---- Deactivate and reactivate ----

    // Only flips IsActive: the account and its comments and reviews are kept (A3).
    // ApplicationSignInManager signs the user out on their next request.
    public async Task<ServiceResult> SetActiveAsync(string id, bool isActive, string currentUserId)
    {
        if (id == currentUserId && !isActive)
        {
            return ServiceResult.Fail(string.Empty, "You cannot deactivate your own account.");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Fail(string.Empty, "This user no longer exists.");
        }

        if (user.DeletedAt is not null)
        {
            return ServiceResult.Fail(string.Empty, DeletedMessage);
        }

        user.IsActive = isActive;
        return await SaveInTransactionAsync(async () => FromIdentity(await userManager.UpdateAsync(user)));
    }

    // ---- Delete and restore ----

    // Moves the account to the Deleted list: hidden from the users list and unable to sign in, but kept
    // with its reviews and comments so it can be restored.
    public async Task<ServiceResult> DeleteAsync(string id, string currentUserId)
    {
        if (id == currentUserId)
        {
            return ServiceResult.Fail(string.Empty, "You cannot delete your own account.");
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Fail(string.Empty, "This user no longer exists.");
        }

        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        return await SaveInTransactionAsync(async () => FromIdentity(await userManager.UpdateAsync(user)));
    }

    // Brings a deleted account back as an active user.
    public async Task<ServiceResult> RestoreAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Fail(string.Empty, "This user no longer exists.");
        }

        user.DeletedAt = null;
        user.IsActive = true;
        return await SaveInTransactionAsync(async () => FromIdentity(await userManager.UpdateAsync(user)));
    }

    // Clears a lockout caused by too many wrong passwords.
    public async Task<ServiceResult> UnlockAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.Fail(string.Empty, "This user no longer exists.");
        }

        return await SaveInTransactionAsync(async () =>
        {
            var result = await userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                result = await userManager.ResetAccessFailedCountAsync(user);
            }

            return FromIdentity(result);
        });
    }

    // ---- Login tracking ----

    // Stamps the time of a successful login, shown as "Last login" on the users list.
    public async Task RecordLoginAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return;
        }

        user.LastLoginAt = DateTime.UtcNow;
        try
        {
            await userManager.UpdateAsync(user);
        }
        catch (DbUpdateException ex)
        {
            // A missing timestamp must never stop someone logging in.
            logger.LogWarning(ex, "Could not record the login time for {UserId}", user.Id);
        }
    }

    // ---- Helpers ----

    private async Task<List<ServiceError>> ValidateAsync(UserFormViewModel model, string? existingUserId)
    {
        var errors = new List<ServiceError>();

        if (!Roles.All.Contains(model.Role))
        {
            errors.Add(new ServiceError(nameof(model.Role), "Pick a valid role."));
        }

        var existing = await userManager.FindByEmailAsync(model.Email.Trim());
        if (existing is not null && existing.Id != existingUserId)
        {
            errors.Add(new ServiceError(nameof(model.Email), "This email is already used by another account."));
        }

        // Every customer belongs to exactly one organisation (A2).
        if (model.Role == Roles.Customer)
        {
            if (model.OrganisationId is null)
            {
                errors.Add(new ServiceError(nameof(model.OrganisationId), "Pick an organisation for this customer."));
            }
            else if (!await db.CustomerOrganisations.AnyAsync(o => o.Id == model.OrganisationId))
            {
                errors.Add(new ServiceError(nameof(model.OrganisationId), "That organisation no longer exists."));
            }
        }

        return errors;
    }

    // Staff accounts never belong to a customer organisation.
    private static int? OrganisationFor(UserFormViewModel model) =>
        model.Role == Roles.Customer ? model.OrganisationId : null;

    // Runs several Identity calls as one unit, so a failure part-way leaves nothing half-saved.
    private async Task<ServiceResult> SaveInTransactionAsync(Func<Task<ServiceResult>> work)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var result = await work();
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }

            return result;
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Saving a user failed");
            return ServiceResult.Fail(string.Empty, SaveFailedMessage);
        }
    }

    private static ServiceResult FromIdentity(IdentityResult result) =>
        result.Succeeded
            ? ServiceResult.Success
            : new ServiceResult(result.Errors.Select(e => new ServiceError(FieldFor(e.Code), e.Description)).ToList());

    private static string FieldFor(string identityErrorCode) => identityErrorCode switch
    {
        _ when identityErrorCode.StartsWith("Password") => nameof(UserFormViewModel.Password),
        "DuplicateEmail" or "DuplicateUserName" or "InvalidEmail" or "InvalidUserName" => nameof(UserFormViewModel.Email),
        _ => string.Empty,
    };

    private async Task<IQueryable<ApplicationUser>> FilterAsync(UserListViewModel filter)
    {
        var query = db.Users.AsNoTracking().Where(u => (u.DeletedAt != null) == filter.Deleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(u => EF.Functions.Like(u.FullName, pattern) || EF.Functions.Like(u.Email!, pattern));
        }

        if (!string.IsNullOrEmpty(filter.Role))
        {
            query = query.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id
                && db.Roles.Any(r => r.Id == ur.RoleId && r.Name == filter.Role)));
        }

        if (filter.IsActive is bool isActive)
        {
            query = query.Where(u => u.IsActive == isActive);
        }

        if (filter.OrganisationId is int organisationId)
        {
            query = query.Where(u => u.OrganisationId == organisationId);
            filter.OrganisationName = await db.CustomerOrganisations
                .Where(o => o.Id == organisationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync();
        }

        return query;
    }

    // Orders by the chosen column, then by name so equal rows keep a stable order.
    private IQueryable<ApplicationUser> Sort(IQueryable<ApplicationUser> query, string? sort, bool desc)
    {
        IOrderedQueryable<ApplicationUser> By<TKey>(Expression<Func<ApplicationUser, TKey>> key) =>
            desc ? query.OrderByDescending(key) : query.OrderBy(key);

        var ordered = sort switch
        {
            UserSort.Email => By(u => u.Email),
            UserSort.Role => By(u => db.UserRoles
                .Where(ur => ur.UserId == u.Id)
                .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .FirstOrDefault()),
            UserSort.Organisation => By(u => u.Organisation != null ? u.Organisation.Name : null),
            UserSort.Status => By(u => u.IsActive),
            UserSort.LastLogin => By(u => u.LastLoginAt),
            _ => By(u => u.FullName),
        };

        return ordered.ThenBy(u => u.FullName).ThenBy(u => u.Email);
    }

    private IQueryable<UserListItem> ToListItems(IQueryable<ApplicationUser> users) =>
        users.Select(u => new UserListItem
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            Role = db.UserRoles
                .Where(ur => ur.UserId == u.Id)
                .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .FirstOrDefault(),
            Organisation = u.Organisation != null ? u.Organisation.Name : null,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            LockoutEnd = u.LockoutEnd,
            DeletedAt = u.DeletedAt,
        });
}
