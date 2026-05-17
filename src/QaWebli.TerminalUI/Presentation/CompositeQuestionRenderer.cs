using QaWebli.Domain.Entities;
using QaWebli.Domain.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.TerminalUI.Presentation;

public sealed class CompositeQuestionRenderer
{
    private readonly IReadOnlyList<IContentRendererStrategy> _renderers;

    public CompositeQuestionRenderer(IEnumerable<IContentRendererStrategy> renderers)
    {
        _renderers = [.. renderers];
    }

    public static CompositeQuestionRenderer Default() =>
        new([new PlainTextRendererStrategy(), new CodeBlockRendererStrategy(), new MathBlockRendererStrategy()]);

    public IRenderable RenderQuestion(Question question)
    {
        var rows = new List<IRenderable>();

        foreach (var block in question.Content)
        {
            var renderer = _renderers.FirstOrDefault(r => r.CanRender(block));
            if (renderer is not null)
                rows.Add(renderer.Render(block));
            else
                rows.Add(new Markup(Markup.Escape(block.ToString() ?? string.Empty)));
        }

        return rows.Count > 0
            ? new Rows(rows)
            : new Markup($"[white]{Markup.Escape(question.RawText)}[/]");
    }
}
