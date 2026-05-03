using QaWebli.Domain.Entities;
using QaWebli.Domain.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.Presentation;

public sealed class PlainTextRendererStrategy : IContentRendererStrategy
{
    public bool CanRender(Question question) => true;

    public IRenderable Render(Question question)
    {
        var rows = new List<IRenderable>();
        foreach (var block in question.Content)
        {
            if (block is ContentBlock.PlainText plain)
                rows.Add(new Markup($"[white]{Markup.Escape(plain.Text)}[/]"));
        }
        return rows.Count > 0
            ? new Rows(rows)
            : new Markup($"[white]{Markup.Escape(question.RawText)}[/]");
    }
}