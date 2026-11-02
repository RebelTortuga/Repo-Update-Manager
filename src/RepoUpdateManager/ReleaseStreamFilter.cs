using System.Text.RegularExpressions;

namespace RepoUpdateManager;

public static class ReleaseStreamFilter
{
    public static bool Matches(string value, string? wildcard)
    {
        if (string.IsNullOrWhiteSpace(wildcard)) return true;
        var expression = "^" + Regex.Escape(wildcard.Trim()).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return Regex.IsMatch(value, expression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

