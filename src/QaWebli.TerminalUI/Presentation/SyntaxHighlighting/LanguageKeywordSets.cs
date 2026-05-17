// ── AI-generated ──────────────────────────────────────────────────────────
// This file was produced with AI assistance. The focus of this project
// (CPIT-252) is on design patterns and architecture; syntax highlighting
// is supporting infrastructure, not the core academic concern.
// ──────────────────────────────────────────────────────────────────────────

namespace QaWebli.Presentation.Rendering.SyntaxHighlighting;

/// <summary>
/// Static <see cref="HashSet{T}"/> of reserved keywords for each supported language.
/// <see cref="GetKeywords"/> normalises the language tag and returns the matching set,
/// or <c>null</c> for unrecognised languages (fallback mode — no keyword colouring).
/// </summary>
public static class LanguageKeywordSets
{
    // ── C# ──────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> CSharp =
    [
        "abstract","as","base","bool","break","byte","case","catch","char","checked",
        "class","const","continue","decimal","default","delegate","do","double","else",
        "enum","event","explicit","extern","false","finally","fixed","float","for",
        "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
        "long","namespace","new","null","object","operator","out","override","params",
        "private","protected","public","readonly","record","ref","return","sbyte","sealed",
        "short","sizeof","stackalloc","static","string","struct","switch","this","throw",
        "true","try","typeof","uint","ulong","unchecked","unsafe","ushort","using",
        "virtual","void","volatile","while","var","async","await","get","set","value",
        "partial","global","required","with","init","nint","nuint","file","scoped",
    ];

    // ── Python ──────────────────────────────────────────────────────────────

    private static readonly HashSet<string> Python =
    [
        "and","as","assert","async","await","break","class","continue","def","del",
        "elif","else","except","False","finally","for","from","global","if","import",
        "in","is","lambda","None","nonlocal","not","or","pass","raise","return",
        "True","try","while","with","yield",
    ];

    // ── Java ────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> Java =
    [
        "abstract","assert","boolean","break","byte","case","catch","char","class",
        "const","continue","default","do","double","else","enum","extends","final",
        "finally","float","for","goto","if","implements","import","instanceof","int",
        "interface","long","native","new","null","package","private","protected",
        "public","return","short","static","strictfp","super","switch","synchronized",
        "this","throw","throws","transient","true","false","try","void","volatile","while","var",
    ];

    // ── JavaScript / TypeScript ─────────────────────────────────────────────

    private static readonly HashSet<string> JavaScript =
    [
        "async","await","break","case","catch","class","const","continue","debugger",
        "default","delete","do","else","export","extends","false","finally","for",
        "from","function","if","import","in","instanceof","let","new","null","of",
        "return","static","super","switch","this","throw","true","try","typeof",
        "undefined","var","void","while","with","yield",
        // Additional web/mermaid keywords to trigger basic colouring
        "graph","flowchart","sequenceDiagram","classDiagram","stateDiagram","pie","gantt",
        "express","app","get","post","put","delete",
    ];

    // ── SQL ─────────────────────────────────────────────────────────────────

    public static readonly HashSet<string> Sql =
    [
        "SELECT","FROM","WHERE","JOIN","LEFT","RIGHT","INNER","OUTER","ON","GROUP",
        "BY","ORDER","HAVING","LIMIT","OFFSET","INSERT","INTO","VALUES","UPDATE",
        "SET","DELETE","CREATE","TABLE","DROP","ALTER","ADD","COLUMN","INDEX",
        "PRIMARY","KEY","FOREIGN","REFERENCES","NOT","NULL","UNIQUE","DEFAULT",
        "AND","OR","IN","IS","LIKE","BETWEEN","AS","DISTINCT","COUNT","SUM","AVG",
        "MAX","MIN","CASE","WHEN","THEN","ELSE","END","EXISTS","UNION","ALL","DESC","ASC",
    ];

    // ── Lookup ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the keyword set for a normalised (lowercase) language tag,
    /// or <c>null</c> when the language is unrecognised.
    /// </summary>
    public static HashSet<string>? GetKeywords(string lang) => lang switch
    {
        "csharp" or "cs" or "c#"                                                                    => CSharp,
        "python" or "py"                                                                             => Python,
        "java"                                                                                       => Java,
        "javascript" or "js" or "typescript" or "ts" or "node" or "express" or "react" or "vue" or "mermaid" => JavaScript,
        "sql"                                                                                        => Sql,
        _                                                                                            => null,
    };
}
