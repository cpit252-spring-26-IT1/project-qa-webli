using QaWebli.Domain.Entities;
using QaWebli.Domain.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.Presentation;

/// Strategy pattern: defines how different types of question content are rendered to the terminal.
/// Implementations decide if they can handle a question and, if so, how to display it.
/// Example: one strategy might render plain text, another might render markdown.
public interface IContentRendererStrategy
{
    /// Checks if this strategy can handle rendering the given question.
    /// Used to select the appropriate renderer at runtime.
    bool CanRender(ContentBlock block);

    /// Renders the question to a Spectre.Console IRenderable for display.
    /// Called only if CanRender() returned true.

    IRenderable Render(ContentBlock block);
}