// AI-Generated Test Suite
using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;

namespace QaWebli.Tests;

public class QuizParserTests
{
    // Resolve quiz path relative to the solution root (3 levels up from test bin output)
    private static string GetQuizPath(string filename)
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        // Navigate from bin/Debug/net8.0 → Tests → src → solution root
        var solutionRoot = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "..", ".."));
        return Path.Combine(solutionRoot, filename);
    }

    [Fact]
    public void ParseFile_ValidMarkdown_ParsesCorrectly()
    {
        var quiz = MarkdownQuizParser.ParseFile(GetQuizPath("math-quiz.md"));

        Assert.Equal("CPIT-252 — Math Equation Testing Quiz", quiz.Title);
        Assert.Equal(2, quiz.TotalQuestions);
    }

    [Fact]
    public void ParseFile_FirstQuestion_HasFourOptions()
    {
        var quiz = MarkdownQuizParser.ParseFile(GetQuizPath("math-quiz.md"));
        var q1 = quiz.Questions[0];

        Assert.Equal(4, q1.Options.Count);
        Assert.Equal("A", q1.Options[0].Label);
        Assert.Equal("B", q1.Options[1].Label);
    }

    [Fact]
    public void ParseFile_CorrectOption_IsMarked()
    {
        var quiz = MarkdownQuizParser.ParseFile(GetQuizPath("math-quiz.md"));
        var q1 = quiz.Questions[0];

        Assert.NotNull(q1.CorrectOption);
        Assert.Equal("B", q1.CorrectOption!.Label);
        Assert.True(q1.CorrectOption.IsCorrect);
    }

    [Fact]
    public void ParseFile_NonexistentFile_Throws()
    {
        Assert.ThrowsAny<Exception>(() =>
            MarkdownQuizParser.ParseFile("nonexistent.md"));
    }
}
