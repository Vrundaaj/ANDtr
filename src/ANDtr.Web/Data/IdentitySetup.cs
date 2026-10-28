using Microsoft.AspNetCore.Identity;

namespace ANDtr.Web.Data;

public static class IdentitySetup
{
    // Account rules shared by the app and the tests: email is the username, and passwords need
    // at least 8 characters with a digit and an upper-case letter.
    public static void ConfigureOptions(IdentityOptions options)
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // Admins create every account, so there is no email confirmation step.
        options.SignIn.RequireConfirmedAccount = false;
    }
}
