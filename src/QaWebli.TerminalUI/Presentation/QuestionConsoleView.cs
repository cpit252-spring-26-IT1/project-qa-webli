using System;
using System.Linq;
using QaWebli.Domain.Entities;
using Spectre.Console;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Responsible solely for rendering active questions, vote statistics, and game mode timers.
/// </summary>
public sealed class QuestionConsoleView
{
    private readonly Session _session;
    private readonly string _joinUrl;
    private readonly CompositeQuestionRenderer _questionRenderer;

    public QuestionConsoleView(Session session, string joinUrl)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _joinUrl = joinUrl;
        _questionRenderer = CompositeQuestionRenderer.Default();
    }

    public void Render()
    {
        var q = _session.CurrentQuestion;
        var votes = _session.GetVoteCounts(_session.CurrentQuestionIndex);
        var totalVotes = votes.Values.Sum();
        AnsiConsole.Clear();

        // ── Question content panel ──────────────────────────────────────────
        var questionContent = _questionRenderer.RenderQuestion(q);

        var panel = new Panel(questionContent)
        {
            Header = new PanelHeader($" [cyan bold]Q{q.Number}[/] / [dim]{_session.Quiz.TotalQuestions}[/] ", Justify.Left),
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
            var check = AnswerRevealedRenderer.GetCheckmark(_session, opt);
            AnsiConsole.MarkupLine($"  [bold {color}]{opt.Label}[/]  {Markup.Escape(opt.Text)}{check}  {bar}  [grey]{count} ({pct:F0}%)[/]");
        }

        AnswerRevealedRenderer.Render(_session, q, votes, totalVotes);

        // ── Footer & QR Code ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(_joinUrl) && !_session.IsGameMode)
        {
            var qrCode = QRGenerator.GenerateCompact(_joinUrl);
            var footerGrid = new Grid().Expand();
            footerGrid.AddColumn(new GridColumn()); // left aligned
            footerGrid.AddColumn(new GridColumn().RightAligned()); // QR on the right

            string gameTimerText = "";
            if (_session.IsGameMode && !_session.IsAnswerRevealed(_session.CurrentQuestionIndex))
            {
                var remaining = Math.Max(0, _session.GameTimerSeconds - (DateTime.UtcNow - _session.GetQuestionStartTime(_session.CurrentQuestionIndex)).TotalSeconds);
                gameTimerText = $"  [pink1 bold]⏱️ {remaining:F1}s remaining[/]\n";
            }

            var info = new Rows(
                new Markup($"[dim]← → Navigate | [bold]Space[/] Reveal | [bold]Q[/] Quit[/]"),
                new Text(""),
                new Markup($"[green]{_session.StudentCount}[/] student(s)  [grey]|[/]  [yellow]{totalVotes}[/] vote(s)\n{gameTimerText}"),
                new Markup($"[link]{Markup.Escape(_joinUrl)}[/]  [dim]← scan to join[/]")
            );

            footerGrid.AddRow(info, new Text(qrCode));
            AnsiConsole.Write(footerGrid);
        }
        else
        {
            string gameTimerText = "";
            if (_session.IsGameMode && !_session.IsAnswerRevealed(_session.CurrentQuestionIndex))
            {
                var remaining = Math.Max(0, _session.GameTimerSeconds - (DateTime.UtcNow - _session.GetQuestionStartTime(_session.CurrentQuestionIndex)).TotalSeconds);
                gameTimerText = $"   [grey]|[/]   [pink1 bold]⏱️ {remaining:F1}s remaining[/]";
            }

            var infoText = _session.IsGameMode
                ? $"[dim]← → Navigate | [bold]Space[/] Reveal | [bold]Q[/] Quit[/]   [grey]|[/]   [green]{_session.StudentCount}[/] student(s)   [grey]|[/]   [yellow]{totalVotes}[/] vote(s){gameTimerText}"
                : $"[dim]← → Navigate | [bold]Space[/] Reveal | [bold]Q[/] Quit[/]   [grey]|[/]   [grey](student UI disabled){gameTimerText}[/]";

            AnsiConsole.MarkupLine(infoText);
        }
    }
}
