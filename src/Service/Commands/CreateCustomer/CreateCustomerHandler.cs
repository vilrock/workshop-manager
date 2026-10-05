using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Commands.CreateCustomer;

public sealed class CreateCustomerHandler(ICustomerRepository customers, IClock clock) : IRequestHandler<CreateCustomerCommand, Result<Customer>>
{
    public async Task<Result<Customer>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var email = TextNormalization.NormalizeEmail(request.Email);

        if (await customers.EmailExistsAsync(email, null, cancellationToken))
        {
            return DomainErrors.Customers.EmailAlreadyExists;
        }

        var now = clock.UtcNow;
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            Address = TextNormalization.NullIfBlank(request.Address),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await customers.CreateAsync(customer, cancellationToken);
        return customer;
    }
}
