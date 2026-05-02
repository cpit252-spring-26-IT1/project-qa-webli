using QaWebli.Domain.Entities;

namespace QaWebli.TerminalUI;

class Program
{
    static void Main(string[] args)
    {
        // ── Option record ──────────────────────────────────────────────────
        var optionA = new Option("A", "Paris", true);
        var optionB = new Option("B", "London", false);
        var optionC = new Option("C", "Berlin", false);

        Console.WriteLine("=== Option (immutable record) ===");
        Console.WriteLine($"  {optionA}");
        Console.WriteLine($"  Value equality: {optionA == new Option("A", "Paris", true)}");

        // ── Question with Builder ──────────────────────────────────────────
        var question = new Question.Builder()
            .WithNumber(1)
            .WithRawText("What is the capital of France?")
            .AddOption("A", "Paris", true)
            .AddOption("B", "London", false)
            .AddOption("C", "Berlin", false)
            .Build();

        Console.WriteLine();
        Console.WriteLine("=== Question (built via Builder) ===");
        Console.WriteLine($"  Q{question.Number}: {question.RawText}");
        foreach (var opt in question.Options)
            Console.WriteLine($"    {opt.Label}) {opt.Text} {(opt.IsCorrect ? "✓" : "")}");
        Console.WriteLine($"  Correct: {question.CorrectOption?.Text}");

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
            .WithTitle("Geography Quiz")
            .AddQuestion(q1)
            .AddQuestion(q2)
            .Build();

        Console.WriteLine($"Quiz: {quiz.Title} ({quiz.TotalQuestions} questions)");
        Console.WriteLine();

        foreach (var q in quiz.Questions)
        {
            Console.WriteLine($"  Q{q.Number}: {q.RawText}");
            foreach (var opt in q.Options)
                Console.WriteLine($"    {opt.Label}) {opt.Text} {(opt.IsCorrect ? "✓" : "")}");
            Console.WriteLine();
        }
    }
}
