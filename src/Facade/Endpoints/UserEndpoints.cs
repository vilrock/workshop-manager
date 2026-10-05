using Facade.Contracts;
using Facade.Http;
using Facade.Mapping;
using Facade.Security;
using MediatR;
using Service.Queries.ListMechanics;

namespace Facade.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this RouteGroupBuilder api)
    {
        var users = api.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization(AuthorizationPolicies.ListMechanics);

        users.MapGet("/mechanics", ListMechanicsAsync)
            .WithName("ListMechanics")
            .WithSummary("Lists active mechanics available for assignment.")
            .Produces<IReadOnlyList<MechanicResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> ListMechanicsAsync(ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListMechanicsQuery(), cancellationToken);
        return result.ToHttpResult(http, mechanics => Results.Ok(mechanics.Select(mechanic => mechanic.ToMechanicResponse()).ToList()));
    }
}
