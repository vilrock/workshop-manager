using System.Security.Cryptography;
using System.Text;
using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderHandler(
    ICurrentUser currentUser,
    IClock clock,
    ICustomerRepository customers,
    IVehicleRepository vehicles,
    IWorkOrderRepository workOrders,
    IIdempotencyRepository idempotency) : IRequestHandler<CreateWorkOrderCommand, Result<CreateWorkOrderResult>>
{
    public async Task<Result<CreateWorkOrderResult>> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role == UserRole.Mechanic)
        {
            return Error.Forbidden("work_orders.create_forbidden", "Mechanics cannot create work orders.");
        }

        var key = request.IdempotencyKey.Trim();
        var fingerprint = ComputeFingerprint(request);
        var now = clock.UtcNow;

        var registered = await idempotency.TryRegisterAsync(key, currentUser.UserId, fingerprint, now, cancellationToken);
        if (!registered)
        {
            return await ReplayAsync(key, fingerprint, cancellationToken);
        }

        var validation = await ValidateReferencesAsync(request, cancellationToken);
        if (!validation.IsSuccess)
        {
            return validation.Error!;
        }

        var workOrderId = Guid.NewGuid();
        var newWorkOrder = new NewWorkOrder(workOrderId, request.CustomerId, request.VehicleId, request.Description.Trim(), request.MileageKm, currentUser.UserId, currentUser.CorrelationId, now);
        await workOrders.CreateAsync(newWorkOrder, cancellationToken);
        await idempotency.AttachWorkOrderAsync(key, currentUser.UserId, workOrderId, cancellationToken);

        return await LoadResultAsync(workOrderId, isReplay: false, cancellationToken);
    }

    private async Task<Result<CreateWorkOrderResult>> ReplayAsync(string key, string fingerprint, CancellationToken cancellationToken)
    {
        var record = await idempotency.GetAsync(key, currentUser.UserId, cancellationToken);

        if (record is null || record.WorkOrderId is null)
        {
            return DomainErrors.WorkOrders.ConcurrentModification;
        }

        if (record.RequestHash != fingerprint)
        {
            return DomainErrors.WorkOrders.IdempotencyKeyReused;
        }

        return await LoadResultAsync(record.WorkOrderId.Value, isReplay: true, cancellationToken);
    }

    private async Task<Result> ValidateReferencesAsync(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var customer = await customers.GetAsync(request.CustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Failure(DomainErrors.Customers.NotFound);
        }

        if (!customer.IsActive)
        {
            return Result.Failure(Error.BusinessRule("customers.inactive", "Work orders cannot be opened for an inactive customer."));
        }

        var vehicle = await vehicles.GetAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure(DomainErrors.Vehicles.NotFound);
        }

        return vehicle.CustomerId == request.CustomerId
            ? Result.Success()
            : Result.Failure(DomainErrors.Vehicles.DoesNotBelongToCustomer);
    }

    private async Task<Result<CreateWorkOrderResult>> LoadResultAsync(Guid workOrderId, bool isReplay, CancellationToken cancellationToken)
    {
        var detail = await workOrders.GetDetailAsync(workOrderId, cancellationToken);
        if (detail is null)
        {
            return DomainErrors.WorkOrders.NotFound;
        }

        return new CreateWorkOrderResult(WorkOrderViewFactory.ToDetailView(detail, currentUser), isReplay);
    }

    private static string ComputeFingerprint(CreateWorkOrderCommand request)
    {
        var canonical = string.Join('|', request.CustomerId.ToString("N"), request.VehicleId.ToString("N"), request.Description.Trim(), request.MileageKm);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
