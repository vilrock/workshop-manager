using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Queries.ListMechanics;

public sealed record ListMechanicsQuery : IRequest<Result<IReadOnlyList<User>>>;
