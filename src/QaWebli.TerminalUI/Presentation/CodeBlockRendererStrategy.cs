using QaWebli.Domain.Entities;
using QaWebli.Domain.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.Presentation;

public sealed class CodeBlockRendererStrategy : IContentRendererStrategy
{
    // quick: handles code blocks only
    public bool CanRender(ContentBlock block) => block is ContentBlock.CodeBlock;

    // quick: show a code block inside a grey panel, escape markup
    public IRenderable Render(ContentBlock block)
    {
        var code = (ContentBlock.CodeBlock)block;

        var header = string.IsNullOrEmpty(code.Language)
            ? null
            : new PanelHeader($" [grey]{Markup.Escape(code.Language)}[/] ", Justify.Right);

        return new Panel(new Markup(Markup.Escape(code.Code)))
        {
            Header = header,
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey42),
            Padding = new Padding(1, 0, 1, 0),
        };
    }
}