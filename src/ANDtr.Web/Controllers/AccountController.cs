using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ANDtr.Web.Controllers;

// Login, logout and the Access denied page for every role (A1, A3).
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    UserService userService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (signInManager.IsSignedIn(User))
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await userService.RecordLoginAsync(model.Email);

            // HomeController sends each role on to its own home page.
            return Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl)
                : RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, await FailureMessageAsync(result, model));
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();

        // Tells the browser to drop its cached copies of pages, including the back / forward
        // cache, so pressing Back after logging out cannot show a signed-in page (A1-03).
        Response.Headers["Clear-Site-Data"] = "\"cache\"";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Says the account is deactivated only when the password was right, so a failed login never
    // reveals which emails have accounts.
    private async Task<string> FailureMessageAsync(SignInResult result, LoginViewModel model)
    {
        if (result.IsLockedOut)
        {
            return "Too many failed attempts. Try again in a few minutes.";
        }

        if (result.IsNotAllowed)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user is { IsActive: false } && await userManager.CheckPasswordAsync(user, model.Password))
            {
                return "Your account is deactivated. Contact your administrator.";
            }
        }

        return "Invalid email or password.";
    }
}
