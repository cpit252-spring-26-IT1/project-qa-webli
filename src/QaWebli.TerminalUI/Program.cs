using QaWebli.Application.Interfaces;
using QaWebli.Application.Services;
using QaWebli.Domain.Events;
using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;

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

        facade.Subscribe(new ConsoleObserver(session));

        RenderQuestion(session);

        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.RightArrow)
            {
                await facade.NextQuestionAsync();
                RenderQuestion(session);
            }
            else if (key.Key == ConsoleKey.LeftArrow)
            {
                await facade.PreviousQuestionAsync();
                RenderQuestion(session);
            }
            else if (key.Key == ConsoleKey.Q)
            {
                break;
            }
        }
    }

    static void RenderQuestion(Session session)
    {
        var q = session.CurrentQuestion;
        Console.Clear();
        Console.WriteLine($"  Q{q.Number}/{session.Quiz.TotalQuestions} — {session.Quiz.Title}");
        Console.WriteLine();
        Console.WriteLine($"  {q.RawText}");
        Console.WriteLine();
        foreach (var opt in q.Options)
            Console.WriteLine($"    {opt.Label}) {opt.Text}");
        Console.WriteLine("\n  ← → Navigate | Q Quit");
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

    public Task OnVoteReceivedAsync(VoteReceivedEvent e) => Task.CompletedTask;
    public Task OnStudentPresenceChangedAsync(StudentPresenceEvent e) => Task.CompletedTask;
}