using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;

namespace QaWebli.TerminalUI;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: qa-webli <quiz.md>");
            return;
        }

        var quiz = MarkdownQuizParser.ParseFile(args[0]);

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