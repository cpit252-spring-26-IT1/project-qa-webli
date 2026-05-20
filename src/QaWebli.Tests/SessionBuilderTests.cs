// AI-Generated Test Suite
using QaWebli.Domain.Entities;

namespace QaWebli.Tests;

public class SessionBuilderTests
{
    private static Quiz CreateSampleQuiz(int questionCount = 2)
    {
        var quizBuilder = new Quiz.Builder().WithTitle("Test Quiz");
        for (int i = 1; i <= questionCount; i++)
        {
            var q = new Question.Builder()
                .WithNumber(i)
                .WithRawText($"Question {i}?")
                .AddOption("A", "Option A", isCorrect: true)
                .AddOption("B", "Option B")
                .Build();
            quizBuilder.AddQuestion(q);
        }
        return quizBuilder.Build();
    }

    [Fact]
    public void Build_WithValidQuiz_CreatesSession()
    {
        var quiz = CreateSampleQuiz();

        var session = new Session.Builder()
            .WithQuiz(quiz)
            .Build();

        Assert.NotNull(session);
        Assert.Equal(quiz, session.Quiz);
        Assert.Equal(0, session.CurrentQuestionIndex);
        Assert.False(session.IsGameMode);
    }

    [Fact]
    public void Build_WithGameMode_SetsGameProperties()
    {
        var quiz = CreateSampleQuiz();

        var session = new Session.Builder()
            .WithQuiz(quiz)
            .WithGameMode(true)
            .WithGameTimerSeconds(15)
            .Build();

        Assert.True(session.IsGameMode);
        Assert.Equal(15, session.GameTimerSeconds);
    }

    [Fact]
    public void Build_WithoutQuiz_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new Session.Builder().Build());
    }

    [Fact]
    public void Build_WithInvalidStartIndex_Throws()
    {
        var quiz = CreateSampleQuiz(2);

        Assert.Throws<InvalidOperationException>(() =>
            new Session.Builder()
                .WithQuiz(quiz)
                .StartingAtQuestion(5)
                .Build());
    }
}
