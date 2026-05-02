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
    }
}
