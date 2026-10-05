namespace Data.Postgres.Repositories;

internal static class LikePattern
{
    public const char EscapeCharacter = '!';

    public static string? FromSearch(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var escaped = search.Trim()
            .Replace("!", "!!", StringComparison.Ordinal)
            .Replace("%", "!%", StringComparison.Ordinal)
            .Replace("_", "!_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
