using Domain.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Util.HealthChecks;

public sealed class PostgresReadinessHealthCheck(IDatabaseProbe probe) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await probe.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("PostgreSQL is reachable.")
            : HealthCheckResult.Unhealthy("PostgreSQL is not reachable.");
}
