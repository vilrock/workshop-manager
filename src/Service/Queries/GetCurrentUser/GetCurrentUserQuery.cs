using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<Result<User>>;
