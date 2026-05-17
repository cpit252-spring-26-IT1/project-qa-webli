// ── AI-generated ──────────────────────────────────────────────────────────
// This file was produced with AI assistance. The focus of this project
// (CPIT-252) is on design patterns and architecture; syntax highlighting
// is supporting infrastructure, not the core academic concern.
// ──────────────────────────────────────────────────────────────────────────

namespace QaWebli.Presentation.Rendering.SyntaxHighlighting;

/// <summary>
/// Detects whether a trimmed line is a single-line comment for the given language.
/// Called before any escaping / highlighting so the entire line can be styled as a comment.
/// </summary>
public static class CommentDetector
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="trimmedLine"/> starts with the
    /// comment token appropriate for <paramref name="lang"/>.
    /// </summary>
    public static bool IsSingleLineComment(string trimmedLine, string lang) =>
        lang is "csharp" or "cs" or "c#" or "java"
            or "javascript" or "js" or "typescript" or "ts"
            or "node" or "express" or "react" or "vue"
            ? trimmedLine.StartsWith("//", StringComparison.Ordinal)
        : lang is "python" or "py"
            ? trimmedLine.StartsWith('#')
        : lang is "sql" or "mermaid"
            ? trimmedLine.StartsWith("--", StringComparison.Ordinal)
              || trimmedLine.StartsWith("%%", StringComparison.Ordinal)
        // Fallback for unknown languages: accept either style
        : trimmedLine.StartsWith("//", StringComparison.Ordinal)
          || trimmedLine.StartsWith('#');
}
