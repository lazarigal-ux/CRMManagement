using System.Diagnostics;
using System.Net.Sockets;

namespace CRMManagement.Launcher;

internal static class Program
{
    // HTTPS is the default because the auth cookie is configured with CookieSecurePolicy.Always
    // (see CRMManagement.Infrastructure/DependencyInjection.cs). On plain HTTP the cookie is
    // dropped after auto-sign-in and the app loops back to /Auth/Login forever.
    private const string DefaultProfile = "https";
    private const string HttpsUrl = "https://localhost:7007/crm";
    private const string HttpUrl = "http://localhost:5064/crm";
    private const int HttpsPort = 7007;
    private const int HttpPort = 5064;

    private static int Main(string[] args)
    {
        var profile = args.Length > 0 ? args[0] : DefaultProfile;
        var openBrowser = !args.Contains("--no-browser", StringComparer.OrdinalIgnoreCase);
        var (browseUrl, browsePort) = profile.Equals("http", StringComparison.OrdinalIgnoreCase)
            ? (HttpUrl, HttpPort)
            : (HttpsUrl, HttpsPort);

        var solutionDir = LocateSolutionDirectory();
        if (solutionDir is null)
        {
            Console.Error.WriteLine("[Launcher] Could not locate CRMManagement.sln from the launcher binary path.");
            return 1;
        }

        var webProject = Path.Combine(solutionDir, "CRMManagement.Web", "CRMManagement.Web.csproj");
        if (!File.Exists(webProject))
        {
            Console.Error.WriteLine($"[Launcher] CRMManagement.Web.csproj not found at: {webProject}");
            return 2;
        }

        Console.WriteLine("======================================================");
        Console.WriteLine(" CRMManagement Launcher  (DEV MODE)");
        Console.WriteLine($"  Solution dir : {solutionDir}");
        Console.WriteLine($"  Web project  : {webProject}");
        Console.WriteLine($"  Launch profile: {profile}");
        Console.WriteLine();
        Console.WriteLine("  Auth bypass: ON — every request signs in as the seeded 'admin' user.");
        Console.WriteLine("  Zoho setup : edit CRMManagement.Web/appsettings.Development.json");
        Console.WriteLine("               -> \"Zoho\": { \"ClientId\": \"...\", \"ClientSecret\": \"...\", \"Region\": \"com\" }");
        Console.WriteLine("               then visit /crm/Admin/Zoho/Import to connect your account.");
        Console.WriteLine();
        Console.WriteLine("  Press Ctrl+C to stop the CRM web app.");
        Console.WriteLine("======================================================");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = solutionDir,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };
        psi.Environment["CRM_DEV_BYPASS_AUTH"] = "1";
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(webProject);
        psi.ArgumentList.Add("--launch-profile");
        psi.ArgumentList.Add(profile);

        Process? web = null;
        try
        {
            web = Process.Start(psi);
            if (web is null)
            {
                Console.Error.WriteLine("[Launcher] Failed to start 'dotnet run' for the web project.");
                return 3;
            }

            if (openBrowser)
            {
                _ = Task.Run(async () =>
                {
                    if (await WaitForPortAsync(browsePort, TimeSpan.FromSeconds(120)))
                    {
                        TryOpenBrowser(browseUrl);
                    }
                    else
                    {
                        Console.WriteLine($"[Launcher] Web app didn't bind port {browsePort} within 120s. Open {browseUrl} manually once the build finishes.");
                    }
                });
            }

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                TryStop(web);
            };

            web.WaitForExit();
            return web.ExitCode;
        }
        finally
        {
            TryStop(web);
        }
    }

    private static string? LocateSolutionDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CRMManagement.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static void TryOpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
            Console.WriteLine($"[Launcher] Opened browser at {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Launcher] Could not open browser ({ex.Message}). Navigate manually to {url}");
        }
    }

    private static void TryStop(Process? p)
    {
        if (p is null || p.HasExited) return;
        try
        {
            p.Kill(entireProcessTree: true);
        }
        catch
        {
            // best-effort
        }
    }

    private static async Task<bool> WaitForPortAsync(int port, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var announced = false;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                await client.ConnectAsync("localhost", port, cts.Token);
                if (client.Connected) return true;
            }
            catch
            {
                if (!announced)
                {
                    Console.WriteLine($"[Launcher] Waiting for the CRM web app to finish building and bind port {port} (first build can take up to a minute)...");
                    announced = true;
                }
            }
            await Task.Delay(800);
        }
        return false;
    }
}
