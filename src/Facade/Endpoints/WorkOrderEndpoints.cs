using Facade.Contracts;
using Facade.Http;
using Facade.Mapping;
using Facade.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Service.Commands.AddLineItem;
using Service.Commands.AssignMechanic;
using Service.Commands.CreateWorkOrder;
using Service.Commands.TransitionWorkOrder;
using Service.Queries.GetWorkOrder;
using Service.Queries.ListWorkOrders;

namespace Facade.Endpoints;

public static class WorkOrderEndpoints
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public const string IdempotentReplayHeader = "Idempotent-Replayed";

    public static void MapWorkOrderEndpoints(this RouteGroupBuilder api)
    {
        var workOrders = api.MapGroup("/work-orders")
            .WithTags("Work orders")
            .RequireAuthorization(AuthorizationPolicies.OperateWorkOrders)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        workOrders.MapGet("/", ListAsync)
            .WithName("ListWorkOrders")
            .WithSummary("Lists work orders. Mechanics only receive the orders assigned to them.")
            .Produces<PagedResponse<WorkOrderSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        workOrders.MapGet("/{workOrderId:guid}", GetAsync)
            .WithName("GetWorkOrder")
            .WithSummary("Gets a work order with line items, history and the actions available to the caller.")
            .Produces<WorkOrderDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        workOrders.MapPost("/", CreateAsync)
            .RequireAuthorization(AuthorizationPolicies.ManageWorkOrders)
            .WithName("CreateWorkOrder")
            .WithSummary("Creates a work order. Requires an Idempotency-Key header; replaying the same key returns the original order.")
            .Produces<WorkOrderDetailResponse>(StatusCodes.Status201Created)
            .Produces<WorkOrderDetailResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        workOrders.MapPut("/{workOrderId:guid}/mechanic", AssignMechanicAsync)
            .RequireAuthorization(AuthorizationPolicies.ManageWorkOrders)
            .WithName("AssignMechanic")
            .WithSummary("Assigns a mechanic to a work order.")
            .Produces<WorkOrderDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        workOrders.MapPost("/{workOrderId:guid}/transitions", TransitionAsync)
            .WithName("TransitionWorkOrder")
            .WithSummary("Moves a work order to a new status. The role needed depends on the transition.")
            .Produces<WorkOrderDetailResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        workOrders.MapPost("/{workOrderId:guid}/items", AddLineItemAsync)
            .WithName("AddLineItem")
            .WithSummary("Adds a service or part line. The total is calculated on the server.")
            .Produces<WorkOrderDetailResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> ListAsync([AsParameters] ListWorkOrdersRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var query = new ListWorkOrdersQuery(request.Status, request.MechanicId, request.Search, request.Page, request.PageSize);
        var result = await sender.Send(query, cancellationToken);
        return result.ToHttpResult(http, page => Results.Ok(page.ToResponse(view => view.ToResponse())));
    }

    private static async Task<IResult> GetAsync(Guid workOrderId, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkOrderQuery(workOrderId), cancellationToken);
        return result.ToHttpResult(http, view => Results.Ok(view.ToResponse()));
    }

    private static async Task<IResult> CreateAsync(
        CreateWorkOrderRequest request,
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        ISender sender,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var command = new CreateWorkOrderCommand(idempotencyKey ?? string.Empty, request.CustomerId, request.VehicleId, request.Description, request.MileageKm);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(http, created => ToCreatedResult(http, created));
    }

    private static async Task<IResult> AssignMechanicAsync(Guid workOrderId, AssignMechanicRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignMechanicCommand(workOrderId, request.MechanicId), cancellationToken);
        return result.ToHttpResult(http, view => Results.Ok(view.ToResponse()));
    }

    private static async Task<IResult> TransitionAsync(Guid workOrderId, TransitionWorkOrderRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new TransitionWorkOrderCommand(workOrderId, request.ToStatus, request.Note, request.ExpectedStatus), cancellationToken);
        return result.ToHttpResult(http, view => Results.Ok(view.ToResponse()));
    }

    private static async Task<IResult> AddLineItemAsync(Guid workOrderId, AddLineItemRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var command = new AddLineItemCommand(workOrderId, request.ItemType, request.Description, request.Quantity, request.UnitPrice);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(http, view => Results.Created($"/api/v1/work-orders/{workOrderId}", view.ToResponse()));
    }

    private static IResult ToCreatedResult(HttpContext http, CreateWorkOrderResult created)
    {
        var response = created.View.ToResponse();

        if (!created.IsReplay)
        {
            return Results.Created($"/api/v1/work-orders/{response.WorkOrderId}", response);
        }

        http.Response.Headers[IdempotentReplayHeader] = "true";
        return Results.Ok(response);
    }
}
