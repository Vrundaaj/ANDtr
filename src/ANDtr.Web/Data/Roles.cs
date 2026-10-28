namespace ANDtr.Web.Data;

public static class Roles
{
    public const string Employee = "Employee";
    public const string Customer = "Customer";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = [Employee, Customer, Admin];
}
