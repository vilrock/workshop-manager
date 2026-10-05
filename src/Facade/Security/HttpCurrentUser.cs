using System.Security.Claims;
using Domain.Abstractions;
using Domain.Enums;
using Facade.Middleware;
using Util.Security;

namespace Facade.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly string fallbackCorrelationId = Guid.NewGuid().ToString("N");

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId => Guid.TryParse(Principal?.FindFirstValue(JwtClaimTypes.Subject), out var userId) ? userId : Guid.Empty;

    public string Email => Principal?.FindFirstValue(JwtClaimTypes.Email) ?? string.Empty;

    public UserRole Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue(JwtClaimTypes.Role), out var role) ? role : UserRole.Mechanic;

    public string CorrelationId => accessor.HttpContext?.Items[CorrelationIdMiddleware.ItemKey] as string ?? fallbackCorrelationId;
}
