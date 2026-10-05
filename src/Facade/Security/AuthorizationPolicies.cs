using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Util.Security;

namespace Facade.Security;

public static class AuthorizationPolicies
{
    public const string ManageCustomers = "ManageCustomers";

    public const string ManageWorkOrders = "ManageWorkOrders";

    public const string OperateWorkOrders = "OperateWorkOrders";

    public const string ViewDashboard = "ViewDashboard";

    public const string ListMechanics = "ListMechanics";

    public static void Configure(AuthorizationOptions options)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        AddRolePolicy(options, ManageCustomers, UserRole.Admin, UserRole.Advisor);
        AddRolePolicy(options, ManageWorkOrders, UserRole.Admin, UserRole.Advisor);
        AddRolePolicy(options, ListMechanics, UserRole.Admin, UserRole.Advisor);
        AddRolePolicy(options, OperateWorkOrders, UserRole.Admin, UserRole.Advisor, UserRole.Mechanic);
        AddRolePolicy(options, ViewDashboard, UserRole.Admin, UserRole.Advisor, UserRole.Mechanic);
    }

    private static void AddRolePolicy(AuthorizationOptions options, string name, params UserRole[] roles) =>
        options.AddPolicy(name, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(JwtClaimTypes.Role, roles.Select(role => role.ToString())));
}
