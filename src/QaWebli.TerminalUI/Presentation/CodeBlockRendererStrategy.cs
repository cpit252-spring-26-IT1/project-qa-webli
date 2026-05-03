using QaWebli.Domain.Entities;
using QaWebli.Domain.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.Presentation;

public sealed class CodeBlockRendererStrategy : IContentRendererStrategy
{

    /// Handles rendering code blocks within question content.
    /// Matches questions that contain any CodeBlock; other renderers may handle text, images, etc.

    public bool CanRender(Question question) => question.Content.Any(b => b is ContentBlock.CodeBlock);

    /// Renders code blocks as styled panels with language headers.
    /// Each block gets its own panel; multiple blocks are stacked vertically.

    public IRenderable Render(Question question)
    {
        var rows = new List<IRenderable>();

        foreach (var block in question.Content)
        {
            if (block is ContentBlock.CodeBlock code)
            {
                // Show language tag if provided; helps readers understand the syntax.
                var header = string.IsNullOrEmpty(code.Language)
                    ? null
                    : new PanelHeader($" [grey]{Markup.Escape(code.Language)}[/] ", Justify.Right);

                // Escape code to prevent markup injection; use grey border for visual hierarchy.
                rows.Add(new Panel(new Markup(Markup.Escape(code.Code)))
                {
                    Header = header,
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Grey42),
                    Padding = new Padding(1, 0, 1, 0),
                });
            }
        }

        // Single block: render directly. Multiple: stack vertically for readability.
        return rows.Count == 1 ? rows[0] : new Rows(rows);
    }
}