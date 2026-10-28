using ANDtr.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin)]
public class ActivityLogController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
