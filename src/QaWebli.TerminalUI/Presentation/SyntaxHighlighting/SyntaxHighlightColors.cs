// ── AI-generated ──────────────────────────────────────────────────────────
// This file was produced with AI assistance. The focus of this project
// (CPIT-252) is on design patterns and architecture; syntax highlighting
// is supporting infrastructure, not the core academic concern.
// ──────────────────────────────────────────────────────────────────────────

namespace QaWebli.Presentation.Rendering.SyntaxHighlighting;

/// <summary>
/// Spectre.Console markup color names for each syntax token category.
/// Centralised here so every highlighter component uses the same palette.
/// </summary>
public static class SyntaxHighlightColors
{
    public const string Keyword = "bold dodgerblue2";
    public const string String  = "gold1";
    public const string Comment = "italic grey50";
    public const string Number  = "chartreuse2";
    public const string Type    = "bold cyan1";
}
