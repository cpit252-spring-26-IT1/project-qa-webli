using QaWebli.Domain.Entities;

namespace QaWebli.TerminalUI;

class Program
{
    static void Main(string[] args)
    {
        var q1 = new Question.Builder()
                .WithNumber(1)
                .WithRawText("What is the capital of France?")
                .AddOption("A", "Paris", true)
                .AddOption("B", "London", false)
                .AddOption("C", "Berlin", false)
                .Build();

        var q2 = new Question.Builder()
                .WithNumber(2)
                .WithRawText("What is the capital of Japan?")
                .AddOption("A", "Seoul", false)
                .AddOption("B", "Beijing", false)
                .AddOption("C", "Tokyo", true)
                .Build();

        var quiz = new Quiz.Builder()
                .WithTitle("Geography Trivia")
                .AddQuestion(q1)
                .AddQuestion(q2)
                .Build();

        var session = new Session.Builder()
                .WithQuiz(quiz)
                .Build();

        Console.WriteLine($"Session {session.Id}: {session.Quiz.Title}");
        Console.WriteLine($"Current: Q{session.CurrentQuestion.Number} - {session.CurrentQuestion.RawText}");

        Console.WriteLine("Move next...");
        session.MoveNext();
        Console.WriteLine($"Current: Q{session.CurrentQuestion.Number} - {session.CurrentQuestion.RawText}");

        Console.WriteLine("Move previous...");
        session.MovePrevious();
        Console.WriteLine($"Current: Q{session.CurrentQuestion.Number} - {session.CurrentQuestion.RawText}");

    }
}
