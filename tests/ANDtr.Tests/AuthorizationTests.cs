using System.Net;
using ANDtr.Web.Data;

namespace ANDtr.Tests;

public sealed class AuthorizationTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    private static readonly string[] EmployeePaths = ["/Search", "/Reviews"];
    private static readonly string[] CustomerPaths = ["/SharedReviews", "/Notifications"];
    private static readonly string[] AdminPaths =
        [
            "/Admin/Users", "/Admin/Users/Create",
            "/Admin/Organisations", "/Admin/Organisations/Create", "/Admin/Dataset", "/Admin/ActivityLog",
        ];

    public static TheoryData<string, string> OwnPaths() => Paths(ownPathsOnly: true);

    public static TheoryData<string, string> ForeignPaths() => Paths(ownPathsOnly: false);

    [Theory]
    [MemberData(nameof(OwnPaths))]
    public async Task Get_WithOwningRole_ReturnsOk(string role, string path)
    {
        var client = await factory.CreateSignedInClientAsync(role);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ForeignPaths))]
    public async Task Get_WithAnotherRole_IsDenied(string role, string path)
    {
        var client = await factory.CreateSignedInClientAsync(role);

        var response = await client.GetAsync(path);

        AssertDenied(response);
    }

    [Theory]
    [InlineData("/Search")]
    [InlineData("/Reviews")]
    [InlineData("/SharedReviews")]
    [InlineData("/Notifications")]
    [InlineData("/Admin/Users")]
    [InlineData("/Admin/Organisations")]
    [InlineData("/Admin/Dataset")]
    [InlineData("/Admin/ActivityLog")]
    public async Task Get_WhenAnonymous_RedirectsToLogin(string path)
    {
        var client = await factory.CreateAnonymousClientAsync();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData(Roles.Employee, "/Search")]
    [InlineData(Roles.Customer, "/SharedReviews")]
    [InlineData(Roles.Admin, "/Admin/Users")]
    public async Task GetRoot_WhenSignedIn_RedirectsToTheRoleLandingPage(string role, string expected)
    {
        var client = await factory.CreateSignedInClientAsync(role);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal(expected, response.Headers.Location?.OriginalString);
    }

    private static TheoryData<string, string> Paths(bool ownPathsOnly)
    {
        var data = new TheoryData<string, string>();

        foreach (var (role, owned) in new[]
        {
            (Roles.Employee, EmployeePaths),
            (Roles.Customer, CustomerPaths),
            (Roles.Admin, AdminPaths),
        })
        {
            var paths = ownPathsOnly
                ? owned
                : EmployeePaths.Concat(CustomerPaths).Concat(AdminPaths).Except(owned);

            foreach (var path in paths)
            {
                data.Add(role, path);
            }
        }

        return data;
    }

    private static void AssertDenied(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return;
        }

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("AccessDenied", response.Headers.Location?.OriginalString);
    }
}
