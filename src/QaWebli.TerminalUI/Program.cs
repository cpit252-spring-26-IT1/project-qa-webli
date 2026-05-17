using QaWebli.Application.Interfaces;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;
using QaWebli.Domain.Events;
using QaWebli.Infrastructure.Logging;
using QaWebli.TerminalUI.Presentation;
using Spectre.Console;
using Spectre.Console.Rendering;
using QaWebli.Infrastructure.Server;
using QaWebli.TerminalUI.Hosting;

namespace QaWebli.TerminalUI;

class Program
{
    static async Task Main(string[] args)
{
        var options = CliOptions.Parse(args);
        if (options is null)
            return;

        var quizPath = options.ResolveQuizPath();
        var quiz = MarkdownQuizParser.ParseFile(quizPath);
        var session = new Session.Builder().WithQuiz(quiz).Build();
        var facade = new SessionFacade(session);

        // Composite + Factory Method — combines all rendering strategies
        var renderer = CompositeQuestionRenderer.Default();

        var (joinUrl, hub, server, tunnel) = await SetupNetworkingAsync(options, facade);

        // Subscribe observers — now renderer and server exist
        facade.Subscribe(new ConsoleObserver(() => RenderQuestion(session, renderer, joinUrl)));
        facade.Subscribe(AuditLogger.Instance);

        AuditLogger.Instance.LogSessionStart(session.Id, session.Quiz.Title);

        RenderQuestion(session, renderer, joinUrl);

        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.RightArrow)
            {
                await facade.NextQuestionAsync();
                RenderQuestion(session, renderer, joinUrl);
            }
            else if (key.Key == ConsoleKey.LeftArrow)
            {
                await facade.PreviousQuestionAsync();
                RenderQuestion(session, renderer, joinUrl);
            }
            else if (key.Key == ConsoleKey.Q)
            {
                break;
            }
        }

        AuditLogger.Instance.LogSessionEnd(session.Id);
        AuditLogger.Instance.Dispose();
        if (hub is not null)
            await hub.CloseAllAsync();
        if (server is not null)
            await server.StopAsync();
        tunnel?.Dispose();
    }

    private static async Task<(string joinUrl, PollingHub? hub, WebServer? server, NgrokTunnel? tunnel)> SetupNetworkingAsync(CliOptions options, SessionFacade facade)
    {
        if (!options.EnableStudentUi)
            return (string.Empty, null, null, null);

        var hub = new PollingHub(facade);
        var server = new WebServer(options.Port, hub);
        await server.StartAsync();

        var scheme = options.Https ? "https" : "http";
        var host = options.Bind.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase) ? server.LocalIp : options.Bind;
        var localJoinUrl = $"{scheme}://{host}:{options.Port}";
        var joinUrl = string.Empty;
        NgrokTunnel? tunnel = null;

        if (options.Ngrok)
        {
            var (ngrokResult, ngrokTunnel) = await NgrokTunnel.StartAsync(options.Port, options.NgrokAuthtoken, preferHttps: true);
            if (!ngrokResult.Success)
            {
                AnsiConsole.MarkupLine($"[red bold]  ✗ ngrok failed:[/] {Markup.Escape(ngrokResult.ErrorMessage ?? "Unknown error")}");
                Environment.Exit(1);
            }
            
            tunnel = ngrokTunnel;
            joinUrl = ngrokResult.PublicUrl!;
            AnsiConsole.MarkupLine($"[green]  ✓[/] Public join (ngrok): [link]{Markup.Escape(joinUrl)}[/]");
        }
        else
        {
            joinUrl = localJoinUrl;
            AnsiConsole.MarkupLine($"[green]  ✓[/] Students join at: [link]{Markup.Escape(joinUrl)}[/]");
        }
        
        await Task.Delay(600); // brief pause so user can see connection status
        return (joinUrl, hub, server, tunnel);
    }


    static void RenderQuestion(Session session, CompositeQuestionRenderer renderer, string joinUrl)
    {
        var q = session.CurrentQuestion;
        var votes = session.GetVoteCounts(session.CurrentQuestionIndex);
        var totalVotes = votes.Values.Sum();
        AnsiConsole.Clear();

        // ── Question content panel ──────────────────────────────────────────
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

        // ── Options with vote bars ──────────────────────────────────────────
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

        // ── Footer & QR Code ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(joinUrl))
        {
            var qrCode = QRGenerator.GenerateCompact(joinUrl);
            var footerGrid = new Grid().Expand();
            footerGrid.AddColumn(new GridColumn()); // left aligned
            footerGrid.AddColumn(new GridColumn().RightAligned()); // QR on the right

            var info = new Rows(
                new Markup($"[dim]← → Navigate | [bold]Q[/] Quit[/]"),
                new Text(""),
                new Markup($"[green]{session.StudentCount}[/] student(s)  [grey]|[/]  [yellow]{totalVotes}[/] vote(s)"),
                new Markup($"[link]{Markup.Escape(joinUrl)}[/]  [dim]← scan to join[/]")
            );

            footerGrid.AddRow(info, new Text(qrCode));
            AnsiConsole.Write(footerGrid);
        }
        else
        {
            AnsiConsole.MarkupLine($"[dim]← → Navigate | [bold]Q[/] Quit[/]   [grey]|[/]   [grey](student UI disabled)[/]");
        }
    }
}