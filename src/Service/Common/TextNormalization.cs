namespace Service.Common;

public static class TextNormalization
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string NormalizeLicensePlate(string licensePlate) => licensePlate.Trim().ToUpperInvariant();

    public static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
