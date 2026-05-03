using QaWebli.Domain.Entities;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.Presentation;

public sealed class PlainTextRendererStrategy : IContentRendererStrategy
{
    public bool CanRender(Question question) => true; // Always can render

    public IRenderable Render(Question question)
    {
        return new Markup($"[white]{Markup.Escape(question.RawText)}[/]");
    }
}