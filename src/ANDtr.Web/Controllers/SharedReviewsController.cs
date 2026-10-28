using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Controllers;

// Customer home and read-only review page. Demo for the privacy checks (A2-05, A2-06) until the
// customer portal is built; the portal should keep loading reviews through SharedReviewService.
[Authorize(Roles = Roles.Customer)]
public class SharedReviewsController(
    OrganisationService organisationService,
    SharedReviewService sharedReviewService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new SharedReviewsHomeViewModel();
        if (await organisationService.GetOrganisationIdAsync(User) is int organisationId)
        {
            model.OrganisationName = (await organisationService.GetForEditAsync(organisationId))?.Name;
            model.Reviews = await sharedReviewService.ListAsync(organisationId);
        }

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var organisationId = await organisationService.GetOrganisationIdAsync(User);
        var review = organisationId is int orgId ? await sharedReviewService.GetAsync(orgId, id) : null;

        // Not shared with this customer's organisation (or not found): Access denied, no data shown.
        return review is null ? Forbid() : View(review);
    }
}
