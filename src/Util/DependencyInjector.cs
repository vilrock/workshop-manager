using Data.Postgres;
using Data.Postgres.Repositories;
using Domain.Abstractions;
using Domain.Repositories;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Service;
using Service.Behaviors;
using Util.HealthChecks;
using Util.Observability;
using Util.Security;
using Util.Time;

namespace Util;

public static class DependencyInjector
{
    public const string ReadinessTag = "ready";

    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        services.AddConfiguration(configuration);
        services.AddPostgres();
        services.AddApplicationServices();
        services.AddSecurityServices();
        services.AddObservability(configuration, serviceName);

        services.AddSingleton<IClock, SystemClock>();
        services.AddHealthChecks().AddCheck<PostgresReadinessHealthCheck>("postgres", tags: [ReadinessTag]);

        return services;
    }

    private static void AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<PostgresOptions>, PostgresOptionsValidator>();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<PostgresOptions>().Bind(configuration.GetSection(PostgresOptions.SectionName)).ValidateOnStart();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateOnStart();
    }

    private static void AddPostgres(this IServiceCollection services)
    {
        services.AddSingleton(provider => NpgsqlDataSource.Create(provider.GetRequiredService<IOptions<PostgresOptions>>().Value.ConnectionString));
        services.AddSingleton<IDatabaseProbe, PostgresDatabaseProbe>();
        services.AddScoped<PostgresSession>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<PostgresSession>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ServiceAssemblyMarker>();
        services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssemblyContaining<ServiceAssemblyMarker>();
            options.AddOpenBehavior(typeof(LoggingBehavior<,>));
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
            options.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });
    }

    private static void AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
    }
}
