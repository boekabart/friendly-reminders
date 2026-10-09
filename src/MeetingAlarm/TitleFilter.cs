using System.Text.RegularExpressions;

/// <summary>
/// Meeting titles to ignore (Meetings.IgnoreTitles), always case-insensitive:
/// "block" = exactly that title, "blok*" = wildcards (* any text, ? one character), "/^x\b/" = a regular expression.
/// </summary>
static class TitleFilter
{
    public static Regex ToRegex(string pattern)
    {
        var p = pattern.Trim();
        var regex = p.Length > 1 && p.StartsWith('/') && p.EndsWith('/') ? p[1..^1]
            : "^" + Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        return new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public static bool Ignored(string title, IEnumerable<string> patterns) =>
        patterns.Any(p => ToRegex(p).IsMatch(title.Trim()));   // ponytail: recompiled per check; cache if the list ever gets long

    /// <summary>A pattern that matches exactly this title, also when it contains * or ?.</summary>
    public static string Exact(string title)
    {
        var t = title.Trim();
        return t.IndexOfAny(['*', '?']) >= 0 || (t.StartsWith('/') && t.EndsWith('/')) ? "/^" + Regex.Escape(t) + "$/" : t;
    }
}
