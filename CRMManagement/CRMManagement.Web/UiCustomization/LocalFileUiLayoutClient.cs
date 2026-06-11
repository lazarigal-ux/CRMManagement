using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CRMManagement.Web.UiCustomization;

/// <summary>
/// Stores UI layout overrides as JSON files under App_Data/ui_layouts_local/.
/// Thread-safe via a static semaphore for file I/O.
/// </summary>
public sealed class LocalFileUiLayoutClient : IUiLayoutClient
{
    private static readonly SemaphoreSlim IoLock = new(1, 1);

    private readonly IHostEnvironment _env;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocalFileUiLayoutClient> _log;

    public LocalFileUiLayoutClient(IHostEnvironment env, IMemoryCache cache, ILogger<LocalFileUiLayoutClient> log)
    {
        _env = env;
        _cache = cache;
        _log = log;
    }

    private string BaseDir => Path.Combine(_env.ContentRootPath, "App_Data", "ui_layouts_local");

    private static string SafeSegment(string value)
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v))
            return "empty_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? "")))[..12];

        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(v.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray())
            .Replace('/', '_').Replace('\\', '_').Replace(':', '_').Replace('?', '_').Replace('&', '_').Replace('=', '_');

        if (safe.Length > 80)
            safe = safe[..80] + "_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v)))[..12];

        return safe;
    }

    private string GetFilePath(string pageKey, string featureKey)
        => Path.Combine(BaseDir, $"{SafeSegment(pageKey)}__{SafeSegment(featureKey)}.json");

    private string CacheKey(string pageKey, string featureKey)
        => $"uilayout:{pageKey}:{featureKey}";

    public async Task<JsonElement?> GetLayoutAsync(string pageKey, string featureKey, CancellationToken ct)
    {
        var ck = CacheKey(pageKey, featureKey);
        if (_cache.TryGetValue<JsonElement>(ck, out var cached))
            return cached;

        var path = GetFilePath(pageKey, featureKey);
        if (!File.Exists(path))
            return null;

        await IoLock.WaitAsync(ct);
        try
        {
            await using var fs = File.OpenRead(path);
            using var doc = await JsonDocument.ParseAsync(fs, cancellationToken: ct);
            var element = doc.RootElement.Clone();
            _cache.Set(ck, element, TimeSpan.FromSeconds(30));
            return element;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[UiLayout] Read failed. Path={Path}", path);
            return null;
        }
        finally
        {
            IoLock.Release();
        }
    }

    public async Task SaveLayoutAsync(string pageKey, string featureKey, JsonElement layoutJson, CancellationToken ct)
    {
        var path = GetFilePath(pageKey, featureKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? BaseDir);

        var json = layoutJson.GetRawText();

        await IoLock.WaitAsync(ct);
        try
        {
            await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
            _cache.Set(CacheKey(pageKey, featureKey), layoutJson.Clone(), TimeSpan.FromSeconds(30));
        }
        finally
        {
            IoLock.Release();
        }
    }

    public async Task ResetLayoutAsync(string pageKey, string featureKey, CancellationToken ct)
    {
        var path = GetFilePath(pageKey, featureKey);
        _cache.Remove(CacheKey(pageKey, featureKey));

        if (!File.Exists(path)) return;

        await IoLock.WaitAsync(ct);
        try
        {
            File.Delete(path);
        }
        finally
        {
            IoLock.Release();
        }
    }
}
