// ── AI-generated ──────────────────────────────────────────────────────────
// This file was produced with AI assistance. The focus of this project
// (CPIT-252) is on design patterns and architecture; ngrok tunnel
// management is supporting infrastructure, not the core academic concern.
// ──────────────────────────────────────────────────────────────────────────

using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace QaWebli.TerminalUI.Hosting;

/// <summary>
/// Structured result from an ngrok tunnel start attempt.
/// <see cref="Success"/> is true only when a matching tunnel was found for the requested port.
/// </summary>
public sealed record NgrokStartResult(bool Success, string? PublicUrl, string? ErrorMessage);

/// <summary>
/// Manages an ngrok subprocess that tunnels a local port to a public HTTPS URL.
///
/// Refactored to return a <see cref="NgrokStartResult"/> so the caller can decide
/// whether to fail fast (when <c>--ngrok</c> is strict) or fall back to LAN.
/// </summary>
public sealed class NgrokTunnel : IDisposable
{
    private readonly Process _process;
    private readonly string? _tempConfigPath;

    public string? PublicUrl { get; }

    private NgrokTunnel(Process process, string? tempConfigPath, string? publicUrl)
    {
        _process = process;
        _tempConfigPath = tempConfigPath;
        PublicUrl = publicUrl;
    }

    /// <summary>
    /// Attempt to start an ngrok tunnel. Returns a structured <see cref="NgrokStartResult"/>
    /// with success/failure information and stderr-aware error messages.
    /// </summary>
    public static async Task<(NgrokStartResult Result, NgrokTunnel? Tunnel)> StartAsync(
        int localPort, string? authtoken, bool preferHttps)
    {
        // Preflight: check ngrok is on PATH
        if (!IsNgrokOnPath())
            return (new NgrokStartResult(false, null, "'ngrok' not found on PATH. Install it from https://ngrok.com/download"), null);

        try
        {
            string? tempConfigPath = null;
            var args = new List<string>();

            if (!string.IsNullOrWhiteSpace(authtoken))
            {
                tempConfigPath = WriteTempConfig(authtoken);
                args.Add("http");
                args.Add($"--config={tempConfigPath}");
                args.Add(localPort.ToString());
            }
            else
            {
                args.Add("http");
                args.Add(localPort.ToString());
            }

            var psi = new ProcessStartInfo
            {
                FileName = "ngrok",
                Arguments = string.Join(' ', args.Select(EscapeArg)),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var process = Process.Start(psi);
            if (process is null)
                return (new NgrokStartResult(false, null, "Failed to start ngrok process."), null);

            // Read stderr asynchronously so we can report it if the tunnel fails
            var stderrTask = process.StandardError.ReadToEndAsync();

            var publicUrl = await TryGetPublicUrlAsync(localPort, preferHttps, timeoutMs: 12_000);

            if (publicUrl is not null)
            {
                var tunnel = new NgrokTunnel(process, tempConfigPath, publicUrl);
                return (new NgrokStartResult(true, publicUrl, null), tunnel);
            }

            // Tunnel did not appear — try to report why
            string? stderr = null;
            if (stderrTask.IsCompleted)
                stderr = await stderrTask;
            else if (process.HasExited)
                stderr = await stderrTask;

            // If the process already exited, it likely failed
            string errorMsg = "ngrok tunnel did not expose a public URL within the timeout.";
            if (!string.IsNullOrWhiteSpace(stderr))
                errorMsg += $"\nngrok stderr: {stderr.Trim()}";
            if (process.HasExited)
                errorMsg += $"\nngrok exited with code {process.ExitCode}.";

            // Clean up since we failed
            try { if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(1500); } } catch { }
            try { if (tempConfigPath is not null && File.Exists(tempConfigPath)) File.Delete(tempConfigPath); } catch { }

            return (new NgrokStartResult(false, null, errorMsg), null);
        }
        catch (Exception ex)
        {
            return (new NgrokStartResult(false, null, $"Failed to start ngrok: {ex.Message}"), null);
        }
    }

    /// <summary>
    /// Legacy convenience wrapper — returns null on failure (same behavior as before).
    /// </summary>
    public static async Task<NgrokTunnel?> TryStartAsync(int localPort, string? authtoken, bool preferHttps)
    {
        var (_, tunnel) = await StartAsync(localPort, authtoken, preferHttps);
        return tunnel;
    }

    public void Dispose()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(1500);
            }
        }
        catch { /* best-effort */ }

        try
        {
            if (!string.IsNullOrWhiteSpace(_tempConfigPath) && File.Exists(_tempConfigPath))
                File.Delete(_tempConfigPath);
        }
        catch { /* best-effort */ }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static bool IsNgrokOnPath()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ngrok",
                Arguments = "version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.WaitForExit(3000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> TryGetPublicUrlAsync(int localPort, bool preferHttps, int timeoutMs)
    {
        // ngrok exposes a local API (default :4040) for tunnel inspection.
        // We poll it briefly until a tunnel matching our port appears.
        using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1500) };
        var deadline = Environment.TickCount64 + timeoutMs;
        var portSuffix = $":{localPort}";

        while (Environment.TickCount64 < deadline)
        {
            try
            {
                var json = await http.GetStringAsync("http://127.0.0.1:4040/api/tunnels");
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("tunnels", out var tunnels) || tunnels.ValueKind != JsonValueKind.Array)
                    return null;

                string? httpUrl = null;
                string? httpsUrl = null;

                foreach (var t in tunnels.EnumerateArray())
                {
                    // Match tunnel to our specific port
                    if (t.TryGetProperty("config", out var cfg)
                        && cfg.TryGetProperty("addr", out var addr)
                        && addr.ValueKind == JsonValueKind.String)
                    {
                        var addrStr = addr.GetString() ?? "";
                        if (!addrStr.EndsWith(portSuffix, StringComparison.OrdinalIgnoreCase)
                            && !addrStr.Equals($"http://localhost{portSuffix}", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    if (!t.TryGetProperty("public_url", out var pu) || pu.ValueKind != JsonValueKind.String)
                        continue;
                    var url = pu.GetString();
                    if (string.IsNullOrWhiteSpace(url))
                        continue;

                    if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        httpsUrl ??= url;
                    else if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                        httpUrl ??= url;
                }

                var matched = preferHttps ? (httpsUrl ?? httpUrl) : (httpUrl ?? httpsUrl);
                if (matched is not null)
                    return matched;
            }
            catch
            {
                // ngrok might not be up yet; retry shortly
                await Task.Delay(350);
            }
        }

        return null;
    }

    private static string WriteTempConfig(string authtoken)
    {
        var dir = Path.Combine(Path.GetTempPath(), "qa-cli");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"ngrok-{Guid.NewGuid():N}.yml");

        // Minimal ngrok config. Token is optional, but when provided we avoid mutating user's global config.
        File.WriteAllText(path, $"version: \"2\"\nauthtoken: {authtoken}\n");
        return path;
    }

    private static string EscapeArg(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";
        if (arg.Any(char.IsWhiteSpace) || arg.Contains('"'))
            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        return arg;
    }
}
