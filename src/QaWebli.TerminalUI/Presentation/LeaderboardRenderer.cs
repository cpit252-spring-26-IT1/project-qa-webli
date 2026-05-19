using System;
using System.Collections.Generic;
using System.Linq;
using QaWebli.Domain.Entities;
using Spectre.Console;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Handles presentation of the final leaderboard, displaying the top three players
/// on a custom podium layout and subsequent participants in a structured table.
/// </summary>
public sealed class LeaderboardRenderer
{
    private readonly Session _session;

    public LeaderboardRenderer(Session session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public void Render()
    {
        AnsiConsole.Clear();

        // Leaderboard Title Header
        var titlePanel = new Panel(new Markup(
            "🎉✨🌟 [yellow bold]✨ L E A D E R B O A R D ✨[/] 🌟✨🎉\n" +
            "   🥳  💥  [pink1 bold]Congratulations to all players! [/] 💥  🥳"
        ))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.DeepPink1_1),
            Padding = new Padding(2, 1, 2, 1)
        };
        AnsiConsole.Write(titlePanel);
        AnsiConsole.WriteLine();

        var sorted = _session.StudentScores.OrderByDescending(kv => kv.Value).ToList();
        if (sorted.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow bold]  No participants scored points in this game! [/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Press Q to exit...[/]");
            return;
        }

        var first = sorted.Count > 0 ? sorted[0] : (KeyValuePair<string, int>?)null;
        var second = sorted.Count > 1 ? sorted[1] : (KeyValuePair<string, int>?)null;
        var third = sorted.Count > 2 ? sorted[2] : (KeyValuePair<string, int>?)null;

        // Custom podium grid
        var podiumGrid = new Grid();
        podiumGrid.AddColumn(new GridColumn().Centered());
        podiumGrid.AddColumn(new GridColumn().Centered());
        podiumGrid.AddColumn(new GridColumn().Centered());

        var p1 = first.HasValue 
            ? new Panel(new Markup($"[yellow bold]👑 1st Place 👑[/]\n\n[yellow bold]{Markup.Escape(first.Value.Key)}[/]\n[bold]{first.Value.Value} pts[/]")) 
                { Border = BoxBorder.Double, BorderStyle = new Style(Color.Gold1), Height = 8, Padding = new Padding(1, 0, 1, 0) }
            : new Panel(new Text("-")) { Border = BoxBorder.None };
            
        var p2 = second.HasValue 
            ? new Panel(new Markup($"[grey bold]🥈 2nd Place 🥈[/]\n\n[grey]{Markup.Escape(second.Value.Key)}[/]\n[bold]{second.Value.Value} pts[/]")) 
                { Border = BoxBorder.Rounded, BorderStyle = new Style(Color.Grey), Height = 7, Padding = new Padding(1, 0, 1, 0) }
            : new Panel(new Text("-")) { Border = BoxBorder.None };

        var p3 = third.HasValue 
            ? new Panel(new Markup($"[rgb(205,127,50) bold]🥉 3rd Place 🥉[/]\n\n[rgb(205,127,50)]{Markup.Escape(third.Value.Key)}[/]\n[bold]{third.Value.Value} pts[/]")) 
                { Border = BoxBorder.Rounded, BorderStyle = new Style(Color.DarkOrange3), Height = 6, Padding = new Padding(1, 0, 1, 0) }
            : new Panel(new Text("-")) { Border = BoxBorder.None };

        podiumGrid.AddRow(p2, p1, p3);
        AnsiConsole.Write(podiumGrid);
        AnsiConsole.WriteLine();

        // Remainder of students table
        if (sorted.Count > 3)
        {
            AnsiConsole.MarkupLine("[bold deepskyblue1]✨ Runners Up: [/]");
            var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.DeepSkyBlue1);
            table.AddColumn("[bold]Rank[/]");
            table.AddColumn("[bold]Student Hash[/]");
            table.AddColumn("[bold]Total Score[/]");

            for (int i = 3; i < sorted.Count; i++)
            {
                table.AddRow(
                    $"#{i + 1}", 
                    $"[cyan]{sorted[i].Key}[/]", 
                    $"[green bold]{sorted[i].Value}[/] pts"
                );
            }
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        // Random confetti row at the bottom
        AnsiConsole.MarkupLine("🎉 * .  ✨  . * 🎉 * .  ✨  . * 🎉 * .  ✨  . * 🎉 * .  ✨  . * 🎉");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Press Q to quit...[/]");
    }
}
