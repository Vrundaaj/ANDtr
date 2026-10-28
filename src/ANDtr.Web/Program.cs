using ANDtr.Web.Data;
using ANDtr.Web.Models;
using ANDtr.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(IdentitySetup.ConfigureOptions)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<ApplicationSignInManager>()
    .AddClaimsPrincipalFactory<ApplicationClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

// Check the user on every request, so a deactivated user is signed out at once (A3).
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Sign out after 30 minutes without activity, so an unattended session cannot be reused.
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<NoticeImporter>();
builder.Services.AddScoped<DevDataSeeder>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<OrganisationService>();
builder.Services.AddScoped<SharedReviewService>();

// Pages are never cached, so the Back button after logout cannot show private data.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DevDataSeeder>().SeedAsync();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
