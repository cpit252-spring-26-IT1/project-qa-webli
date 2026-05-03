using QaWebli.Application.Interfaces;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;
using QaWebli.Domain.Events;
using QaWebli.Infrastructure.Logging;
using QaWebli.Presentation;
using Spectre.Console;

namespace QaWebli.TerminalUI;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: qa-webli <quiz.md>");
            return;
        }

        var quiz = MarkdownQuizParser.ParseFile(args[0]);
        var session = new Session.Builder().WithQuiz(quiz).Build();
        var facade = new SessionFacade(session);

        // Subscribe observers
        facade.Subscribe(new ConsoleObserver(session));
        facade.Subscribe(AuditLogger.Instance);

        AuditLogger.Instance.LogSessionStart(session.Id, session.Quiz.Title);

        // Strategy pattern — choose how to render question content
        IContentRendererStrategy renderer = new PlainTextRendererStrategy();

        RenderQuestion(session, renderer);

        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.RightArrow)
            {
                await facade.NextQuestionAsync();
                RenderQuestion(session, renderer);
            }
            else if (key.Key == ConsoleKey.LeftArrow)
            {
                await facade.PreviousQuestionAsync();
                RenderQuestion(session, renderer);
            }
            else if (key.Key == ConsoleKey.Q)
            {
                break;
            }
        }

        AuditLogger.Instance.LogSessionEnd(session.Id);
        AuditLogger.Instance.Dispose();
    }

    // needed AI here the implemetation took a while of trail and error to get right, especially the console rendering with Spectre.Console

    static void RenderQuestion(Session session, IContentRendererStrategy renderer)
    {
        var q = session.CurrentQuestion;
        AnsiConsole.Clear();

        // Question body in a panel with a colored border
        var questionContent = renderer.CanRender(q) ? renderer.Render(q) : new Markup(Markup.Escape(q.RawText));

        var panel = new Panel(questionContent)
        {
            Header = new PanelHeader($" [cyan bold]Q{q.Number}[/] / [dim]{session.Quiz.TotalQuestions}[/] ", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(1, 0, 1, 0),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Options with letter badges
        foreach (var opt in q.Options)
        {
            var color = opt.IsCorrect ? "green" : "white";
            var check = opt.IsCorrect ? " ✓" : "";
            AnsiConsole.MarkupLine($"  [{color}]  [bold]{opt.Label}[/]  {Markup.Escape(opt.Text)}{check}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]← → Navigate | [bold]Q[/] Quit[/]");
    }
}

public class ConsoleObserver : ISessionObserver
{
    private readonly Session _session;

    public ConsoleObserver(Session session) { _session = session; }

    public Task OnQuestionChangedAsync(QuestionChangedEvent e)
    {
        Console.WriteLine($"  [Event] Question changed to {e.QuestionIndex + 1}/{e.TotalQuestions}");
        return Task.CompletedTask;
    }

    // not yet implemented!
    public Task OnVoteReceivedAsync(VoteReceivedEvent e) => Task.CompletedTask;
    public Task OnStudentPresenceChangedAsync(StudentPresenceEvent e) => Task.CompletedTask;
}