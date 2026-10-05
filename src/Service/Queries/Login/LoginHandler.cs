using Domain.Abstractions;
using Domain.Common;
using Domain.Repositories;
using MediatR;
using Service.Common;

namespace Service.Queries.Login;

public sealed class LoginHandler(IUserRepository users, IPasswordHasher passwordHasher, ITokenService tokenService) : IRequestHandler<LoginQuery, Result<LoginResult>>
{
    public async Task<Result<LoginResult>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetByEmailAsync(TextNormalization.NormalizeEmail(request.Email), cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return DomainErrors.Auth.InvalidCredentials;
        }

        if (!user.IsActive)
        {
            return DomainErrors.Auth.InactiveUser;
        }

        return new LoginResult(tokenService.Create(user), user);
    }
}
