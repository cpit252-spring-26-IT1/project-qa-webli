using QaWebli.Domain.Entities;
using QaWebli.Domain.ValueObjects;
using QaWebli.Presentation.Rendering.SyntaxHighlighting;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.TerminalUI.Presentation;

public sealed class CodeBlockRendererStrategy : IContentRendererStrategy
{
    // quick: handles code blocks only
    public bool CanRender(ContentBlock block) => block is ContentBlock.CodeBlock;

    // Renders a code block with syntax-highlighted content inside a grey panel
    public IRenderable Render(ContentBlock block)
    {
        var code = (ContentBlock.CodeBlock)block;
        var lang = (code.Language ?? string.Empty).ToLowerInvariant();
        var highlighted = EscapeFirstSyntaxHighlighter.HighlightSafe(code.Code, lang);

        var header = string.IsNullOrEmpty(lang)
            ? null
            : new PanelHeader($" [grey50]{Markup.Escape(lang)}[/] ", Justify.Right);

        return new Panel(highlighted)
        {
            Header = header,
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey42),
            Padding = new Padding(1, 0, 1, 0),
        };
    }
}