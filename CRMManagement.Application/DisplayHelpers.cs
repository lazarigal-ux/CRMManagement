namespace CRMManagement.Application;

/// <summary>
/// Shared formatting and display helpers used across multiple pages.
/// Eliminates duplicated <c>BuildIssueKey</c>, <c>BuildInitials</c>, and <c>HumanizeWhen</c> methods.
/// </summary>
public static class DisplayHelpers
{
    /// <summary>
    /// Builds a Jira-style issue key like "PROJ-42".
    /// Returns <c>null</c> when the inputs are invalid.
    /// </summary>
    public static string? BuildIssueKey(string? projectKey, int number)
    {
        if (string.IsNullOrWhiteSpace(projectKey) || number <= 0)
            return null;

        return projectKey + "-" + number;
    }

    /// <summary>
    /// Extracts 1–2 character initials from a display name.
    /// </summary>
    public static string BuildInitials(string? name)
    {
        var v = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(v)) return "U";

        var parts = v.Split(new[] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0][..1].ToUpperInvariant();

        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    /// <summary>
    /// Returns a human-readable relative timestamp ("just now", "about 3 hours ago", "2 days ago", etc.).
    /// </summary>
    public static string HumanizeWhen(DateTime when)
    {
        var delta = DateTime.UtcNow - when;
        if (delta.TotalMinutes < 2) return "just now";
        if (delta.TotalMinutes < 60) return $"about {Math.Floor(delta.TotalMinutes)} minutes ago";
        if (delta.TotalHours < 24) return $"about {Math.Floor(delta.TotalHours)} hours ago";
        if (delta.TotalDays < 2) return "yesterday";
        return $"{Math.Floor(delta.TotalDays)} days ago";
    }

    /// <summary>
    /// Normalizes an <c>IssueHistory.FieldName</c> to a human-friendly label.
    /// </summary>
    public static string NormalizeFieldName(string? fieldName)
    {
        var f = (fieldName ?? string.Empty).Trim();
        if (string.Equals(f, "StatusId", StringComparison.OrdinalIgnoreCase)) return "status";
        if (string.Equals(f, "SprintId", StringComparison.OrdinalIgnoreCase)) return "sprint";
        return f;
    }
}
