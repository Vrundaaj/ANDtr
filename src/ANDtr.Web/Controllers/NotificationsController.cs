using ANDtr.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Controllers;

[Authorize(Roles = Roles.Customer)]
public class NotificationsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
