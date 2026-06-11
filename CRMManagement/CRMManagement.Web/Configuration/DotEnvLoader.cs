using System.Text;

namespace CRMManagement.Web.Configuration;

public static class DotEnvLoader
{
    public static void LoadIfMissing(string envFilePath, params string[] keys)
    {
        if (keys is null || keys.Length == 0)
        {
            return;
        }

        var anyMissing = keys.Any(k => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(k)));
        if (!anyMissing)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(envFilePath) || !File.Exists(envFilePath))
        {
            return;
        }

        foreach (var (key, value) in ReadKeyValues(envFilePath))
        {
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!keys.Contains(key, StringComparer.Ordinal)) continue;

            var current = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(current)) continue;

            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static IEnumerable<(string Key, string Value)> ReadKeyValues(string envFilePath)
    {
        foreach (var rawLine in File.ReadLines(envFilePath, Encoding.UTF8))
        {
            var line = rawLine?.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith('#')) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();

            if (value.Length >= 2)
            {
                if ((value.StartsWith('"') && value.EndsWith('"')) || (value.StartsWith('\'') && value.EndsWith('\'')))
                {
                    value = value[1..^1];
                }
            }

            yield return (key, value);
        }
    }
}
