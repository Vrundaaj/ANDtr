using System.Diagnostics;
using ANDtr.Web.Data;
using ANDtr.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(Roles.Employee))
        {
            return RedirectToAction("Index", "Search");
        }

        if (User.IsInRole(Roles.Customer))
        {
            return RedirectToAction("Index", "SharedReviews");
        }

        if (User.IsInRole(Roles.Admin))
        {
            return RedirectToAction("Index", "Users", new { area = "Admin" });
        }

        return RedirectToAction("Login", "Account");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
