using QaWebli.Domain.Entities;
using QaWebli.Domain.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.TerminalUI.Presentation;

public sealed class PlainTextRendererStrategy : IContentRendererStrategy
{
    public bool CanRender(ContentBlock block) => block is ContentBlock.PlainText;

    public IRenderable Render(ContentBlock block)
    {
        var plain = (ContentBlock.PlainText)block;
        return new Markup($"[white]{Markup.Escape(plain.Text)}[/]");
    }
}