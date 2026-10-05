using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Commands.UpdateCustomer;

public sealed class UpdateCustomerHandler(ICustomerRepository customers, IClock clock) : IRequestHandler<UpdateCustomerCommand, Result<Customer>>
{
    public async Task<Result<Customer>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await customers.GetAsync(request.CustomerId, cancellationToken);
        if (existing is null)
        {
            return DomainErrors.Customers.NotFound;
        }

        var email = TextNormalization.NormalizeEmail(request.Email);
        if (await customers.EmailExistsAsync(email, request.CustomerId, cancellationToken))
        {
            return DomainErrors.Customers.EmailAlreadyExists;
        }

        var updated = new Customer
        {
            CustomerId = existing.CustomerId,
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            Address = TextNormalization.NullIfBlank(request.Address),
            IsActive = request.IsActive,
            VehicleCount = existing.VehicleCount,
            CreatedAtUtc = existing.CreatedAtUtc,
            UpdatedAtUtc = clock.UtcNow
        };

        return await customers.UpdateAsync(updated, cancellationToken) ? updated : DomainErrors.Customers.NotFound;
    }
}
