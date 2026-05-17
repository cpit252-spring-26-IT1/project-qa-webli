// ── AI-generated ──────────────────────────────────────────────────────────
// This file was produced with AI assistance. The focus of this project
// (CPIT-252) is on design patterns and architecture; syntax highlighting
// is supporting infrastructure, not the core academic concern.
// ──────────────────────────────────────────────────────────────────────────

using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.Presentation.Rendering.SyntaxHighlighting;

/// <summary>
/// Escape-first syntax highlighter. Produces a Spectre.Console <see cref="IRenderable"/>
/// with keyword / string / comment / number / type colouring.
///
/// <b>Algorithm (per line):</b>
/// <list type="number">
///   <item>Detect full-line comments → grey italic escaped line.</item>
///   <item>Extract string literals to GUID-keyed placeholders → <see cref="Markup.Escape"/> remainder.</item>
///   <item>Word-boundary regex pass: keywords (per language set), PascalCase type names, numbers.</item>
///   <item>Restore string placeholders with gold colour.</item>
///   <item>Wrap in <see cref="Markup"/> with try/catch fallback to plain <see cref="Text"/> if markup is still invalid.</item>
/// </list>
/// </summary>
public sealed partial class EscapeFirstSyntaxHighlighter
{
    // Source-generated regex for matching word boundaries in ESCAPED text
    [GeneratedRegex(@"\b(\w+)\b")]
    private static partial Regex WordBoundary();

    /// <summary>
    /// Highlight the given <paramref name="code"/> for the given <paramref name="lang"/>
    /// and return a safe Spectre <see cref="IRenderable"/>.
    /// </summary>
    public static IRenderable HighlightSafe(string code, string lang)
    {
        var keywords = LanguageKeywordSets.GetKeywords(lang);
        var sb = new StringBuilder();
        var lines = code.Split('\n');

        foreach (var line in lines)
        {
            sb.Append(HighlightLineSafe(line, keywords, lang));
            sb.Append('\n');
        }

        var result = sb.ToString().TrimEnd('\n');

        // Fallback: if the markup is somehow still broken, return plain text
        try
        {
            return new Markup(result);
        }
        catch
        {
            return new Text(code);
        }
    }

    // ── Per-line pipeline ───────────────────────────────────────────────────

    private static string HighlightLineSafe(string line, HashSet<string>? keywords, string lang)
    {
        // Step 0: detect full-line comments before escaping
        var trimmed = line.TrimStart();
        if (CommentDetector.IsSingleLineComment(trimmed, lang))
            return $"[{SyntaxHighlightColors.Comment}]{Markup.Escape(line)}[/]";

        // Step 1: Extract strings and replace with safe placeholders
        var stringRanges = StringLiteralScanner.FindStringRanges(line);
        var strings = new List<string>();
        var tempLine = new StringBuilder();
        int lastPos = 0;
        string marker = "__STR_MARKER_" + Guid.NewGuid().ToString("N") + "_";

        foreach (var (start, end) in stringRanges)
        {
            tempLine.Append(line, lastPos, start - lastPos);
            string placeholder = $"{marker}{strings.Count}_";
            tempLine.Append(placeholder);
            strings.Add(line.Substring(start, end - start));
            lastPos = end;
        }
        tempLine.Append(line, lastPos, line.Length - lastPos);

        // Step 2: Escape the line without strings
        var workLine = tempLine.ToString();
        var escaped = Markup.Escape(workLine);

        // Step 3: Colourize Keywords & Types
        var result = WordBoundary().Replace(escaped, m =>
        {
            var word = m.Value;
            if (word.StartsWith("__STR_MARKER_")) return word; // ignore placeholders

            bool isKw = keywords != null && (keywords.Contains(word)
                     || (lang == "sql" && LanguageKeywordSets.Sql.Contains(word.ToUpperInvariant())));
            if (isKw)
                return $"[{SyntaxHighlightColors.Keyword}]{word}[/]";

            // PascalCase type names (at least 2 chars, first uppercase)
            if (word.Length > 1 && char.IsUpper(word[0]))
                return $"[{SyntaxHighlightColors.Type}]{word}[/]";

            return word;
        });

        // Step 4: Colourize numbers (regex avoids matching inside tags or placeholders)
        result = Regex.Replace(result, @"\b\d+\.?\d*\b", m =>
        {
            return $"[{SyntaxHighlightColors.Number}]{m.Value}[/]";
        });

        // Step 5: Restore strings with colour
        for (int i = 0; i < strings.Count; i++)
        {
            var escapedStr = Markup.Escape(strings[i]);
            var coloredStr = $"[{SyntaxHighlightColors.String}]{escapedStr}[/]";
            result = result.Replace($"{marker}{i}_", coloredStr);
        }

        return result;
    }
}
