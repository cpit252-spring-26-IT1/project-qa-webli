// ── AI-generated ──────────────────────────────────────────────────────────
// This file was produced with AI assistance. The focus of this project
// (CPIT-252) is on design patterns and architecture; QR code rendering
// is supporting infrastructure, not the core academic concern.
// ──────────────────────────────────────────────────────────────────────────

using QRCoder;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Generates ASCII QR codes suitable for terminal display using Unicode block characters.
/// Uses QRCoder's <see cref="AsciiQRCode"/> renderer with error correction level M.
/// On any failure, returns a safe fallback string instead of throwing.
/// </summary>
public static class QRGenerator
{
    /// <summary>Compact borderless QR code for status panels.</summary>
    public static string GenerateCompact(string url)
    {
        try
        {
            if (IsNgrokUrl(url))
                return GenerateHalfBlock(url);

            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
            using var qr = new AsciiQRCode(data);
            return qr.GetGraphic(1, drawQuietZones: false);
        }
        catch
        {
            return "[QR unavailable]";
        }
    }

    private static bool IsNgrokUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Host.Contains("ngrok", StringComparison.OrdinalIgnoreCase);

    private static string GenerateHalfBlock(string url)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.L);

        var modules = data.ModuleMatrix;
        var lines = new List<string>((modules.Count + 1) / 2);

        for (var row = 0; row < modules.Count; row += 2)
        {
            var line = new char[modules.Count];
            for (var column = 0; column < modules.Count; column++)
            {
                var top = modules[row][column];
                var bottom = row + 1 < modules.Count && modules[row + 1][column];
                line[column] = (top, bottom) switch
                {
                    (true, true) => '█',
                    (true, false) => '▀',
                    (false, true) => '▄',
                    _ => ' '
                };
            }

            lines.Add(new string(line));
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Full QR code with quiet zone for wide terminals.</summary>
    public static string Generate(string url)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
            using var qr = new AsciiQRCode(data);
            return qr.GetGraphic(1, drawQuietZones: true);
        }
        catch
        {
            return "[QR unavailable]";
        }
    }
}
