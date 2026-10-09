namespace iDoctor.Api.Extensions;

public static class SearchExtensions
{
    // Builds a safe "contains" pattern for ILIKE: escapes the characters
    // that have special meaning in LIKE patterns (\ % _)
    public static string ToContainsPattern(this string input)
    {
        var escaped = input.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
        return $"%{escaped}%";
    }
}