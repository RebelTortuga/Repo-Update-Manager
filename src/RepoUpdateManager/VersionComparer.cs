using System.Text.RegularExpressions;

namespace RepoUpdateManager;

public static partial class VersionComparer
{
    // A pragmatic SemVer-aware comparer. Non-version text remains stable and is compared ordinally.
    public static int Compare(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left)) return string.IsNullOrWhiteSpace(right) ? 0 : -1;
        if (string.IsNullOrWhiteSpace(right)) return 1;
        var l = Normalize(left); var r = Normalize(right);
        if (Version.TryParse(l.Core, out var lv) && Version.TryParse(r.Core, out var rv))
        {
            var core = lv.CompareTo(rv);
            if (core != 0) return core;
            if (l.Pre is null && r.Pre is not null) return 1;
            if (l.Pre is not null && r.Pre is null) return -1;
            return string.Compare(l.Pre, r.Pre, StringComparison.OrdinalIgnoreCase);
        }
        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUpdate(string? installed, string? available) => Compare(available, installed) > 0;

    private static (string Core, string? Pre) Normalize(string value)
    {
        var match = SemVer().Match(value.Trim());
        return match.Success ? (match.Groups["core"].Value, match.Groups["pre"].Success ? match.Groups["pre"].Value : null) : (value, null);
    }
    [GeneratedRegex("^v?(?<core>\\d+(?:\\.\\d+){0,3})(?:-(?<pre>[0-9A-Za-z.-]+))?(?:\\+[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex SemVer();
}

