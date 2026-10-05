using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Commands.UpdateCustomer;

public sealed record UpdateCustomerCommand(Guid CustomerId, string FullName, string Email, string Phone, string? Address, bool IsActive) : IRequest<Result<Customer>>, ITransactionalCommand;
