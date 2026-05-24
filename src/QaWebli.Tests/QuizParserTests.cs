// AI-Generated Test Suite
using QaWebli.Domain.Entities;
using QaWebli.Infrastructure.Parsing;

namespace QaWebli.Tests;

public class QuizParserTests
{
    private static string GetQuizPath(string filename)
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        var solutionRoot = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "..", ".."));
        return Path.Combine(solutionRoot, filename);
    }

    [Fact]
    public void ParseFile_LoadsQuizTitleQuestionCountAndCorrectOption()
    {
        var quiz = MarkdownQuizParser.ParseFile(GetQuizPath("math-quiz.md"));
        var firstQuestion = quiz.Questions[0];

        Assert.Equal("CPIT-252 — Math Equation Testing Quiz", quiz.Title);
        Assert.Equal(2, quiz.TotalQuestions);
        Assert.Equal(4, firstQuestion.Options.Count);
        Assert.Equal("B", firstQuestion.CorrectOption?.Label);
    }

    [Fact]
    public void Parse_RejectsMissingTitle()
    {
        const string markdown = "## Question 1: Prompt\n- [x] A) Answer";

        Assert.Throws<FormatException>(() => MarkdownQuizParser.Parse(markdown));
    }

    [Fact]
    public void Parse_RejectsMissingQuestionContent()
    {
        Assert.Throws<InvalidOperationException>(() => MarkdownQuizParser.Parse("# Empty Quiz"));

        const string noOptions = "# Quiz\n\n## Question 1: Prompt";
        Assert.Throws<InvalidOperationException>(() => MarkdownQuizParser.Parse(noOptions));
    }
}
