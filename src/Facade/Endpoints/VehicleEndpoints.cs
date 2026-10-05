using Facade.Contracts;
using Facade.Http;
using Facade.Mapping;
using Facade.Security;
using MediatR;
using Service.Commands.CreateVehicle;
using Service.Commands.UpdateVehicle;
using Service.Queries.ListCustomerVehicles;
using Service.Queries.ListVehicles;

namespace Facade.Endpoints;

public static class VehicleEndpoints
{
    public static void MapVehicleEndpoints(this RouteGroupBuilder api)
    {
        var vehicles = api.MapGroup(string.Empty)
            .WithTags("Vehicles")
            .RequireAuthorization(AuthorizationPolicies.ManageCustomers)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        vehicles.MapGet("/customers/{customerId:guid}/vehicles", ListByCustomerAsync)
            .WithName("ListCustomerVehicles")
            .WithSummary("Lists the vehicles that belong to a customer.")
            .Produces<IReadOnlyList<VehicleResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        vehicles.MapPost("/customers/{customerId:guid}/vehicles", CreateAsync)
            .WithName("CreateVehicle")
            .WithSummary("Registers a vehicle for a customer.")
            .Produces<VehicleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        vehicles.MapGet("/vehicles", ListAsync)
            .WithName("ListVehicles")
            .WithSummary("Searches vehicles with pagination.")
            .Produces<PagedResponse<VehicleResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        vehicles.MapPut("/vehicles/{vehicleId:guid}", UpdateAsync)
            .WithName("UpdateVehicle")
            .WithSummary("Updates a vehicle.")
            .Produces<VehicleResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListByCustomerAsync(Guid customerId, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListCustomerVehiclesQuery(customerId), cancellationToken);
        return result.ToHttpResult(http, vehicles => Results.Ok(vehicles.Select(vehicle => vehicle.ToResponse()).ToList()));
    }

    private static async Task<IResult> ListAsync([AsParameters] ListVehiclesRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListVehiclesQuery(request.Search, request.CustomerId, request.Page, request.PageSize), cancellationToken);
        return result.ToHttpResult(http, page => Results.Ok(page.ToResponse(vehicle => vehicle.ToResponse())));
    }

    private static async Task<IResult> CreateAsync(Guid customerId, VehicleRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var command = new CreateVehicleCommand(customerId, request.LicensePlate, request.Make, request.Model, request.Year, request.Vin, request.Color, request.MileageKm);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(http, vehicle => Results.Created($"/api/v1/vehicles/{vehicle.VehicleId}", vehicle.ToResponse()));
    }

    private static async Task<IResult> UpdateAsync(Guid vehicleId, VehicleRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var command = new UpdateVehicleCommand(vehicleId, request.LicensePlate, request.Make, request.Model, request.Year, request.Vin, request.Color, request.MileageKm);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(http, vehicle => Results.Ok(vehicle.ToResponse()));
    }
}
