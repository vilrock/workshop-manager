using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Commands.CreateCustomer;

public sealed record CreateCustomerCommand(string FullName, string Email, string Phone, string? Address) : IRequest<Result<Customer>>, ITransactionalCommand;
