using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Areas.Admin.Controllers;

// Admin users: list, create, edit (A1), deactivate / reactivate (A3), delete / restore and unlock.
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class UsersController(
    UserService userService,
    OrganisationService organisationService,
    UserManager<ApplicationUser> userManager) : Controller
{
    public async Task<IActionResult> Index(UserListViewModel filter)
    {
        var model = await userService.SearchAsync(filter);
        return View(model);
    }

    // ---- Create ----

    // role and organisationId pre-fill the form, e.g. from "Add a customer user" on an organisation.
    public async Task<IActionResult> Create(string? role, int? organisationId)
    {
        var model = new UserFormViewModel
        {
            Role = Roles.All.Contains(role) ? role! : Roles.Employee,
            OrganisationId = organisationId,
        };

        return await FormViewAsync(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        model.Id = null;
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model);
        }

        var result = await userService.CreateAsync(model);
        if (!result.Succeeded)
        {
            result.CopyTo(ModelState);
            return await FormViewAsync(model);
        }

        TempData["Success"] = $"User “{model.FullName}” was created.";
        return RedirectToAction(nameof(Index));
    }

    // ---- Edit ----

    public async Task<IActionResult> Edit(string id)
    {
        var model = await userService.GetForEditAsync(id);
        return model is null ? NotFound() : await FormViewAsync(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model);
        }

        var result = await userService.UpdateAsync(model, userManager.GetUserId(User)!);
        if (!result.Succeeded)
        {
            result.CopyTo(ModelState);
            return await FormViewAsync(model);
        }

        TempData["Success"] = $"User “{model.FullName}” was updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---- Deactivate / reactivate / unlock ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Deactivate(string id) =>
        ChangeAccountAsync(id, "deactivated", () => userService.SetActiveAsync(id, isActive: false, userManager.GetUserId(User)!));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivate(string id) =>
        ChangeAccountAsync(id, "reactivated", () => userService.SetActiveAsync(id, isActive: true, userManager.GetUserId(User)!));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Delete(string id) =>
        ChangeAccountAsync(id, "deleted", () => userService.DeleteAsync(id, userManager.GetUserId(User)!));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Restore(string id) =>
        ChangeAccountAsync(id, "restored", () => userService.RestoreAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unlock(string id) =>
        ChangeAccountAsync(id, "unlocked", () => userService.UnlockAsync(id));

    // ---- Helpers ----

    private async Task<IActionResult> ChangeAccountAsync(string id, string pastTense, Func<Task<ServiceResult>> change)
    {
        var user = await userService.GetForEditAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var result = await change();
        if (result.Succeeded)
        {
            TempData["Success"] = $"User “{user.FullName}” was {pastTense}.";
        }
        else
        {
            TempData["Error"] = result.Errors[0].Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormViewAsync(UserFormViewModel model)
    {
        model.OrganisationOptions = await organisationService.GetOptionsAsync();
        return View(model.IsEdit ? nameof(Edit) : nameof(Create), model);
    }
}
