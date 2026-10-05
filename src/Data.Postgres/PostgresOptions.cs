using Microsoft.Extensions.Options;

namespace Data.Postgres;

public sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    public const int DefaultCommandTimeoutSeconds = 30;

    public const int MaxCommandTimeoutSeconds = 600;

    public string ConnectionString { get; set; } = string.Empty;

    public int CommandTimeoutSeconds { get; set; } = DefaultCommandTimeoutSeconds;
}

public sealed class PostgresOptionsValidator : IValidateOptions<PostgresOptions>
{
    public ValidateOptionsResult Validate(string? name, PostgresOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Postgres:ConnectionString is required.");
        }

        if (options.CommandTimeoutSeconds is < 1 or > PostgresOptions.MaxCommandTimeoutSeconds)
        {
            return ValidateOptionsResult.Fail($"Postgres:CommandTimeoutSeconds must be between 1 and {PostgresOptions.MaxCommandTimeoutSeconds}.");
        }

        return ValidateOptionsResult.Success;
    }
}
