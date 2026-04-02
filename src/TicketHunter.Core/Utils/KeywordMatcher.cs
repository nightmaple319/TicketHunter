namespace TicketHunter.Core.Utils;

/// <summary>
/// Keyword matching engine.
/// Semicolons (;) separate OR groups. Spaces within a group act as AND.
/// First matching group wins (priority ordering).
/// </summary>
public static class KeywordMatcher
{
    /// <summary>
    /// Check if text matches the keyword expression.
    /// </summary>
    public static bool IsMatch(string text, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return true;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var orGroups = keyword.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var group in orGroups)
        {
            var andTerms = group.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (andTerms.Length == 0) continue;

            var allMatch = true;
            foreach (var term in andTerms)
            {
                if (text.IndexOf(term.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch) return true;
        }

        return false;
    }

    /// <summary>
    /// From a list of options, return the first matching item based on keyword.
    /// Returns null if no match found.
    /// </summary>
    public static string? FindFirstMatch(IEnumerable<string> options, string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return options.FirstOrDefault();

        var orGroups = keyword.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var group in orGroups)
        {
            var trimmedGroup = group.Trim();
            if (string.IsNullOrEmpty(trimmedGroup)) continue;

            foreach (var option in options)
            {
                if (IsMatchSingleGroup(option, trimmedGroup))
                    return option;
            }
        }

        return null;
    }

    /// <summary>
    /// From a list of options, return all matching items based on keyword,
    /// excluding items that match the exclude keyword.
    /// </summary>
    public static List<string> FindAllMatches(IEnumerable<string> options, string keyword, string? excludeKeyword = null)
    {
        var results = new List<string>();
        var optionList = options.ToList();

        foreach (var option in optionList)
        {
            if (!string.IsNullOrWhiteSpace(excludeKeyword) && IsMatch(option, excludeKeyword))
                continue;

            if (IsMatch(option, keyword))
                results.Add(option);
        }

        return results;
    }

    /// <summary>
    /// Select an item based on mode from matched options.
    /// Modes: from_top, from_bottom, center, random
    /// </summary>
    public static string? SelectByMode(List<string> options, string mode)
    {
        if (options.Count == 0) return null;

        return mode.ToLowerInvariant() switch
        {
            "from_bottom" => options[^1],
            "center" => options[options.Count / 2],
            "random" => options[Random.Shared.Next(options.Count)],
            _ => options[0] // from_top (default)
        };
    }

    private static bool IsMatchSingleGroup(string text, string group)
    {
        var andTerms = group.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var term in andTerms)
        {
            if (text.IndexOf(term.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }
        return true;
    }
}
