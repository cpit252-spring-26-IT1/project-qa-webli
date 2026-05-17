// ── AI-generated ──────────────────────────────────────────────────────────
// This file was produced with AI assistance. The focus of this project
// (CPIT-252) is on design patterns and architecture; syntax highlighting
// is supporting infrastructure, not the core academic concern.
// ──────────────────────────────────────────────────────────────────────────

namespace QaWebli.Presentation.Rendering.SyntaxHighlighting;

/// <summary>
/// Scans a source line for string literal ranges delimited by <c>"</c> or <c>'</c>.
/// Back-slash escapes (<c>\"</c>, <c>\'</c>) are respected so interior quotes do
/// not close the literal prematurely.
/// Returns a list of <c>[start, end)</c> index pairs into the original line.
/// </summary>
public static class StringLiteralScanner
{
    /// <summary>Find spans [start, end) of string literals in the original line.</summary>
    public static List<(int start, int end)> FindStringRanges(string line)
    {
        var ranges = new List<(int, int)>();
        int i = 0;
        while (i < line.Length)
        {
            if (line[i] is '"' or '\'')
            {
                char q = line[i];
                int start = i++;
                while (i < line.Length && line[i] != q)
                {
                    if (line[i] == '\\') i++;
                    i++;
                }
                if (i < line.Length) i++; // closing quote
                ranges.Add((start, i));
            }
            else
            {
                i++;
            }
        }
        return ranges;
    }
}
