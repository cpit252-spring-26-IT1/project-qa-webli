using System;
using System.Threading.Tasks;
using QaWebli.Application.Services;
using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;
using QaWebli.Infrastructure.Logging;
using QaWebli.Infrastructure.Server;
using QaWebli.TerminalUI.Hosting;
using QaWebli.TerminalUI.Presentation;
using Spectre.Console;

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
        var session = new Session.Builder()
            .WithQuiz(quiz)
            .WithGameMode(options.Game)
            .WithGameTimerSeconds(options.GameTimerSeconds)
            .Build();
        var facade = new SessionFacade(session);

        var (joinUrl, hub, server, tunnel) = await SetupNetworkingAsync(options, facade);

        // Subscribe global audit logger observer
        AuditLogger.Initialize(session);
        facade.Subscribe(AuditLogger.Instance);
        AuditLogger.Instance.LogSessionStart(session.Id, session.Quiz.Title);

        // Instantiate and run the presenter controller
        var presenter = new PresenterController(session, facade, joinUrl);
        await presenter.RunAsync();

        // Clean up resources upon exit
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
}