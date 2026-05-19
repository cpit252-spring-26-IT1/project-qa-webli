using System;
using QaWebli.Domain.Entities;
using Spectre.Console;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Responsible solely for rendering the presenter's game lobby screen.
/// </summary>
public sealed class LobbyConsoleView
{
    private readonly Session _session;
    private readonly string _joinUrl;

    public LobbyConsoleView(Session session, string joinUrl)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _joinUrl = joinUrl;
    }

    public void Render()
    {
        AnsiConsole.Clear();
        
        var rule = new Rule("[purple bold]🎮 QA-CLI Game Lobby 🎮[/]")
        {
            Style = Style.Parse("purple")
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var lobbyPanel = new Panel(
            new Align(
                new Rows(
                    new Markup("[bold yellow]Waiting for all players to join...[/]"),
                    new Text(""),
                    new Markup($"[bold green]👥 Players Joined: {_session.StudentCount}[/]"),
                    new Text("")
                ),
                HorizontalAlignment.Center
            )
        )
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Purple),
            Padding = new Padding(3, 1, 3, 1),
            Width = 60
        };
        
        AnsiConsole.Write(new Align(lobbyPanel, HorizontalAlignment.Center));
        AnsiConsole.WriteLine();

        if (!string.IsNullOrWhiteSpace(_joinUrl))
        {
            var qrCode = QRGenerator.GenerateCompact(_joinUrl);
            var grid = new Grid().Expand();
            grid.AddColumn(new GridColumn()); // instructions
            grid.AddColumn(new GridColumn().RightAligned()); // QR

            var info = new Rows(
                new Markup("[bold cyan]Scan the QR code to join on your device[/]"),
                new Text(""),
                new Markup($"Join URL: [link]{Markup.Escape(_joinUrl)}[/]"),
                new Text(""),
                new Markup("[bold underline magenta]Press [[Enter]] to Start the Game[/]"),
                new Markup("[dim]Press [[Q]] to Quit[/]")
            );

            grid.AddRow(info, new Text(qrCode));
            AnsiConsole.Write(grid);
        }
        else
        {
            AnsiConsole.MarkupLine("[bold underline magenta]Press [[Enter]] to Start the Game[/]");
            AnsiConsole.MarkupLine("[dim]Press [[Q]] to Quit[/]");
        }
    }
}
