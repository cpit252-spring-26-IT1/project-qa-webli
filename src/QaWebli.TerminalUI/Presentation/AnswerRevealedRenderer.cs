using System;
using System.Collections.Generic;
using System.Linq;
using QaWebli.Domain.Entities;
using Spectre.Console;

namespace QaWebli.TerminalUI.Presentation;

public static class AnswerRevealedRenderer
{
    public static string GetCheckmark(Session session, Option opt)
    {
        var isRevealed = session.IsAnswerRevealed(session.CurrentQuestionIndex);
        return (isRevealed && opt.IsCorrect) ? " [green bold]✓[/]" : string.Empty;
    }

    public static void Render(Session session, Question q, IReadOnlyDictionary<string, int> votes, int totalVotes)
    {
        var isRevealed = session.IsAnswerRevealed(session.CurrentQuestionIndex);
        if (isRevealed && q.CorrectOption != null)
        {
            var correctVotes = votes.GetValueOrDefault(q.CorrectOption.Label, 0);
            var pctCorrect = totalVotes > 0 ? (correctVotes * 100.0 / totalVotes) : 0;

            var resultsPanel = new Panel(
                new Markup($"Correct Option: [bold green]{q.CorrectOption.Label}[/] ({Markup.Escape(q.CorrectOption.Text)})\n" +
                           $"Total Participants (Votes): [bold]{totalVotes}[/]\n" +
                           $"Got It Right: [bold green]{correctVotes}[/] ({pctCorrect:F0}%)")
            )
            {
                Header = new PanelHeader("[bold green]Answer Revealed[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Padding = new Padding(1, 0, 1, 0)
            };
            AnsiConsole.Write(resultsPanel);
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.WriteLine();
        }
    }
}
