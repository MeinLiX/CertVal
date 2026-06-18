namespace CertVal.Application.Common.Certificates;

/// <summary>
/// Pure helpers for merging certificate tag sets used by bulk tag operations.
/// Case-insensitive de-duplication, trimming and empty removal, mirroring the
/// normalization performed by the Certificate entity.
/// </summary>
public static class TagMerge
{
    public static List<string> Add(IEnumerable<string> current, IEnumerable<string> toAdd)
    {
        var result = current.ToList();
        foreach (var tag in toAdd)
        {
            var trimmed = tag.Trim();
            if (trimmed.Length == 0) continue;
            if (!result.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
                result.Add(trimmed);
        }
        return result;
    }

    public static List<string> Remove(IEnumerable<string> current, IEnumerable<string> toRemove)
    {
        var remove = new HashSet<string>(toRemove.Select(t => t.Trim()), StringComparer.OrdinalIgnoreCase);
        return current.Where(x => !remove.Contains(x)).ToList();
    }
}
