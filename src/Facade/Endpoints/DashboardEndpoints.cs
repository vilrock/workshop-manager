using Facade.Contracts;
using Facade.Http;
using Facade.Mapping;
using Facade.Security;
using MediatR;
using Service.Queries.GetDashboardStats;

namespace Facade.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this RouteGroupBuilder api)
    {
        var dashboard = api.MapGroup("/dashboard")
            .WithTags("Dashboard")
            .RequireAuthorization(AuthorizationPolicies.ViewDashboard);

        dashboard.MapGet("/stats", GetStatsAsync)
            .WithName("GetDashboardStats")
            .WithSummary("Returns order counts, revenue and average times. Mechanics only see their own orders.")
            .Produces<DashboardStatsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetStatsAsync(ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDashboardStatsQuery(), cancellationToken);
        return result.ToHttpResult(http, view => Results.Ok(view.ToResponse()));
    }
}
