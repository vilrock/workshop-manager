using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using MediatR;

namespace Service.Queries.Login;

public sealed record LoginQuery(string Email, string Password) : IRequest<Result<LoginResult>>;

public sealed record LoginResult(AccessToken Token, User User);
