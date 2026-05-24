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
            {
                var reshapedStr = ArabicHelper.Reshape(block.ToString() ?? string.Empty);
                var markup = new Markup(Markup.Escape(reshapedStr));
                rows.Add(ArabicHelper.ContainsArabic(reshapedStr) ? new Align(markup, HorizontalAlignment.Right) : markup);
            }
        }

        if (rows.Count > 0)
            return new Rows(rows);
            
        var fallbackStr = ArabicHelper.Reshape(question.RawText);
        var fallbackMarkup = new Markup($"[white]{Markup.Escape(fallbackStr)}[/]");
        return ArabicHelper.ContainsArabic(question.RawText) ? new Align(fallbackMarkup, HorizontalAlignment.Right) : fallbackMarkup;
    }
}
