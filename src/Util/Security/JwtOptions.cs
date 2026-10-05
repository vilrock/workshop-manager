using Microsoft.Extensions.Options;

namespace Util.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinimumSecretLength = 32;

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpiresMinutes { get; set; } = 60;
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (options.Secret.Length < JwtOptions.MinimumSecretLength)
        {
            return ValidateOptionsResult.Fail($"Jwt:Secret must contain at least {JwtOptions.MinimumSecretLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Jwt:Issuer and Jwt:Audience are required.");
        }

        return options.ExpiresMinutes is < 1 or > 1440
            ? ValidateOptionsResult.Fail("Jwt:ExpiresMinutes must be between 1 and 1440.")
            : ValidateOptionsResult.Success;
    }
}
