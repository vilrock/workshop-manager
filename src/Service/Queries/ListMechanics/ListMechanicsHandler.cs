using Domain.Abstractions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Repositories;
using MediatR;

namespace Service.Queries.ListMechanics;

public sealed class ListMechanicsHandler(ICurrentUser currentUser, IUserRepository users) : IRequestHandler<ListMechanicsQuery, Result<IReadOnlyList<User>>>
{
    public async Task<Result<IReadOnlyList<User>>> Handle(ListMechanicsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role == UserRole.Mechanic)
        {
            return Error.Forbidden("users.forbidden", "Mechanics cannot list other staff members.");
        }

        return Result<IReadOnlyList<User>>.Success(await users.ListActiveMechanicsAsync(cancellationToken));
    }
}
