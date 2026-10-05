using Facade.Contracts;
using Facade.Http;
using Facade.Mapping;
using Facade.Security;
using MediatR;
using Service.Commands.CreateCustomer;
using Service.Commands.UpdateCustomer;
using Service.Queries.GetCustomer;
using Service.Queries.ListCustomers;

namespace Facade.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this RouteGroupBuilder api)
    {
        var customers = api.MapGroup("/customers")
            .WithTags("Customers")
            .RequireAuthorization(AuthorizationPolicies.ManageCustomers)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        customers.MapGet("/", ListAsync)
            .WithName("ListCustomers")
            .WithSummary("Searches customers with pagination.")
            .Produces<PagedResponse<CustomerResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        customers.MapGet("/{customerId:guid}", GetAsync)
            .WithName("GetCustomer")
            .WithSummary("Gets a customer by id.")
            .Produces<CustomerResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        customers.MapPost("/", CreateAsync)
            .WithName("CreateCustomer")
            .WithSummary("Creates a customer.")
            .Produces<CustomerResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        customers.MapPut("/{customerId:guid}", UpdateAsync)
            .WithName("UpdateCustomer")
            .WithSummary("Updates a customer.")
            .Produces<CustomerResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> ListAsync([AsParameters] ListCustomersRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListCustomersQuery(request.Search, request.Page, request.PageSize), cancellationToken);
        return result.ToHttpResult(http, page => Results.Ok(page.ToResponse(customer => customer.ToResponse())));
    }

    private static async Task<IResult> GetAsync(Guid customerId, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCustomerQuery(customerId), cancellationToken);
        return result.ToHttpResult(http, customer => Results.Ok(customer.ToResponse()));
    }

    private static async Task<IResult> CreateAsync(CreateCustomerRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCustomerCommand(request.FullName, request.Email, request.Phone, request.Address), cancellationToken);
        return result.ToHttpResult(http, customer => Results.Created($"/api/v1/customers/{customer.CustomerId}", customer.ToResponse()));
    }

    private static async Task<IResult> UpdateAsync(Guid customerId, UpdateCustomerRequest request, ISender sender, HttpContext http, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateCustomerCommand(customerId, request.FullName, request.Email, request.Phone, request.Address, request.IsActive), cancellationToken);
        return result.ToHttpResult(http, customer => Results.Ok(customer.ToResponse()));
    }
}
