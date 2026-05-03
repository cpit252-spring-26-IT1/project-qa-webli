using QaWebli.Application.Interfaces;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;
using QaWebli.Domain.Events;
using QaWebli.Infrastructure.Logging;
using QaWebli.Presentation;
using Spectre.Console;
using QaWebli.Infrastructure.Server;

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

        // Composite + Factory Method — combines all rendering strategies
        var renderer = CompositeQuestionRenderer.Default();

        // Start web server so students can connect from their phones
        var hub = new PollingHub(facade);
        var server = new WebServer(8080, hub);
        await server.StartAsync();
        AnsiConsole.MarkupLine($"[green]  ✓[/] Students join at: [link]{server.ServerUrl}[/]");
        await Task.Delay(600);

        // Subscribe observers — now renderer and server exist
        facade.Subscribe(new ConsoleObserver(session, () => RenderQuestion(session, renderer, server.ServerUrl)));
        facade.Subscribe(AuditLogger.Instance);

        AuditLogger.Instance.LogSessionStart(session.Id, session.Quiz.Title);

        RenderQuestion(session, renderer, server.ServerUrl);

        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.RightArrow)
            {
                await facade.NextQuestionAsync();
                RenderQuestion(session, renderer, server.ServerUrl);
            }
            else if (key.Key == ConsoleKey.LeftArrow)
            {
                await facade.PreviousQuestionAsync();
                RenderQuestion(session, renderer, server.ServerUrl);
            }
            else if (key.Key == ConsoleKey.Q)
            {
                break;
            }
        }

        AuditLogger.Instance.LogSessionEnd(session.Id);
        AuditLogger.Instance.Dispose();
        await hub.CloseAllAsync();
        await server.StopAsync();
    }

    // needed AI here the implemetation took a while of trail and error to get right, especially the console rendering with Spectre.Console

    static void RenderQuestion(Session session, CompositeQuestionRenderer renderer, string serverUrl)
    {
        var q = session.CurrentQuestion;
        var votes = session.GetVoteCounts(session.CurrentQuestionIndex);
        var totalVotes = votes.Values.Sum();
        AnsiConsole.Clear();

        var questionContent = renderer.RenderQuestion(q);

        var panel = new Panel(questionContent)
        {
            Header = new PanelHeader($" [cyan bold]Q{q.Number}[/] / [dim]{session.Quiz.TotalQuestions}[/] ", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(1, 0, 1, 0),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Options with vote bars
        var palette = new[] { "green", "aqua", "yellow", "red", "purple", "teal" };
        foreach (var (opt, i) in q.Options.Select((o, idx) => (o, idx)))
        {
            var color = palette[i % palette.Length];
            var count = votes.GetValueOrDefault(opt.Label, 0);
            var pct = totalVotes > 0 ? count * 100.0 / totalVotes : 0;
            var barWidth = 20;
            var filled = (int)Math.Round(pct / 100.0 * barWidth);
            var empty = barWidth - filled;

            var bar = $"[{color}]" + new string('#', filled) + new string('-', empty) + "[/]";
            /// removed the "✓" for later to add now I want to see without it.
            var check = opt.IsCorrect ? "" : "";
            AnsiConsole.MarkupLine($"  [bold {color}]{opt.Label}[/]  {Markup.Escape(opt.Text)}{check}  {bar}  [grey]{count} ({pct:F0}%)[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]← → Navigate | [bold]Q[/] Quit[/]   [grey]|[/]   [green]{session.StudentCount}[/] student(s)  [grey]|[/]  [yellow]{totalVotes}[/] vote(s)  [link]{serverUrl}[/]");
    }
}

public class ConsoleObserver : ISessionObserver
{
    private readonly Session _session;
    private readonly Action _onVote;

    public ConsoleObserver(Session session, Action onVote)
    {
        _session = session;
        _onVote = onVote;
    }

    public Task OnQuestionChangedAsync(QuestionChangedEvent e)
    {
        Console.WriteLine($"  [Event] Question changed to {e.QuestionIndex + 1}/{e.TotalQuestions}");
        return Task.CompletedTask;
    }

    public Task OnVoteReceivedAsync(VoteReceivedEvent e)
    {
        _onVote();
        return Task.CompletedTask;
    }

    public Task OnStudentPresenceChangedAsync(StudentPresenceEvent e)
    {
        _onVote();
        return Task.CompletedTask;
    }
}