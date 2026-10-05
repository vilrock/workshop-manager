using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using Domain.Repositories;
using MediatR;

namespace Service.Queries.GetCurrentUser;

public sealed class GetCurrentUserHandler(ICurrentUser currentUser, IUserRepository users) : IRequestHandler<GetCurrentUserQuery, Result<User>>
{
    public async Task<Result<User>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return DomainErrors.Auth.NotAuthenticated;
        }

        var user = await users.GetByIdAsync(currentUser.UserId, cancellationToken);
        return user is { IsActive: true } ? user : DomainErrors.Auth.UserNotFound;
    }
}
