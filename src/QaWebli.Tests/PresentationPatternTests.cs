using QaWebli.Domain.Events;
using QaWebli.Domain.ValueObjects;
using QaWebli.TerminalUI.Presentation;
using Spectre.Console.Rendering;

namespace QaWebli.Tests;

public class PresentationPatternTests
{
    [Fact]
    public async Task ConsoleObserver_TriggersRenderCallback()
    {
        var renderCalls = 0;
        var observer = new ConsoleObserver(() => renderCalls++);

        await observer.OnVoteReceivedAsync(VoteReceivedEvent.Now(0, "student-1", "A"));

        Assert.Equal(1, renderCalls);
    }

    [Fact]
    public void PlainTextRendererStrategy_RendersOnlyPlainTextBlocks()
    {
        var strategy = new PlainTextRendererStrategy();

        Assert.True(strategy.CanRender(new ContentBlock.PlainText("hello")));
        Assert.False(strategy.CanRender(new ContentBlock.CodeBlock("csharp", "return;")));
        Assert.IsAssignableFrom<IRenderable>(strategy.Render(new ContentBlock.PlainText("hello")));
    }
}
