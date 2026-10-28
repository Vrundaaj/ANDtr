using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Areas.Admin.Controllers;

// Customer organisations: list, create, edit and delete (A2).
[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class OrganisationsController(OrganisationService organisationService, UserService userService) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        ViewData["Search"] = search;
        return View(await organisationService.ListAsync(search));
    }

    // ---- Create ----

    public IActionResult Create()
    {
        return View(new OrganisationFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrganisationFormViewModel model)
    {
        model.Id = null;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await organisationService.CreateAsync(model);
        if (!result.Succeeded)
        {
            result.CopyTo(ModelState);
            return View(model);
        }

        TempData["Success"] = $"Organisation “{model.Name}” was created.";
        return RedirectToAction(nameof(Index));
    }

    // ---- Edit ----

    public async Task<IActionResult> Edit(int id)
    {
        var model = await organisationService.GetForEditAsync(id);
        return model is null ? NotFound() : await EditViewAsync(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrganisationFormViewModel model)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return await EditViewAsync(model);
        }

        var result = await organisationService.UpdateAsync(model);
        if (!result.Succeeded)
        {
            result.CopyTo(ModelState);
            return await EditViewAsync(model);
        }

        TempData["Success"] = $"Organisation “{model.Name}” was updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---- Delete ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await organisationService.DeleteAsync(id);
        if (result.Succeeded)
        {
            TempData["Success"] = "Organisation deleted.";
        }
        else
        {
            TempData["Error"] = result.Errors[0].Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> EditViewAsync(OrganisationFormViewModel model)
    {
        model.Customers = await userService.GetOrganisationMembersAsync(model.Id!.Value);
        return View(nameof(Edit), model);
    }
}
