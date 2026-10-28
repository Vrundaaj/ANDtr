using ANDtr.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ANDtr.Web.Controllers;

[Authorize(Roles = Roles.Employee)]
public class SearchController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
