// AI-Generated Test Suite
using QaWebli.Domain.Entities;

namespace QaWebli.Tests;

public class SessionNavigationTests
{
    private static Session CreateSession(int questionCount = 3)
    {
        var quizBuilder = new Quiz.Builder().WithTitle("Nav Test");
        for (int i = 1; i <= questionCount; i++)
        {
            quizBuilder.AddQuestion(new Question.Builder()
                .WithNumber(i)
                .WithRawText($"Q{i}?")
                .AddOption("A", "Ans A", isCorrect: true)
                .AddOption("B", "Ans B")
                .Build());
        }
        return new Session.Builder().WithQuiz(quizBuilder.Build()).Build();
    }

    [Fact]
    public void TryMoveNext_FromFirst_MovesToSecond()
    {
        var session = CreateSession();

        bool moved = session.TryMoveNext(out int newIndex);

        Assert.True(moved);
        Assert.Equal(1, newIndex);
        Assert.Equal(1, session.CurrentQuestionIndex);
    }

    [Fact]
    public void TryMoveNext_AtLastQuestion_ReturnsFalse()
    {
        var session = CreateSession(2);
        session.TryMoveNext(out _); // move to index 1 (last)

        bool moved = session.TryMoveNext(out int newIndex);

        Assert.False(moved);
        Assert.Equal(1, newIndex); // stays at 1
    }

    [Fact]
    public void TryMovePrevious_FromSecond_MovesToFirst()
    {
        var session = CreateSession();
        session.TryMoveNext(out _); // now at 1

        bool moved = session.TryMovePrevious(out int newIndex);

        Assert.True(moved);
        Assert.Equal(0, newIndex);
    }

    [Fact]
    public void TryMovePrevious_AtFirst_ReturnsFalse()
    {
        var session = CreateSession();

        bool moved = session.TryMovePrevious(out int newIndex);

        Assert.False(moved);
        Assert.Equal(0, newIndex);
    }

    [Fact]
    public void RevealAnswer_ValidQuestion_ReturnsTrue()
    {
        var session = CreateSession();

        bool revealed = session.RevealAnswer(0);

        Assert.True(revealed);
        Assert.True(session.IsAnswerRevealed(0));
    }

    [Fact]
    public void RevealAnswer_CalledTwice_ReturnsFalseSecondTime()
    {
        var session = CreateSession();

        session.RevealAnswer(0);
        bool second = session.RevealAnswer(0);

        Assert.False(second); // already revealed
    }

    [Fact]
    public void StudentCount_TracksAddAndRemove()
    {
        var session = CreateSession();

        session.AddStudent("s1");
        session.AddStudent("s2");
        Assert.Equal(2, session.StudentCount);

        session.RemoveStudent("s1");
        Assert.Equal(1, session.StudentCount);
    }

    [Fact]
    public void GetStudentRank_ReturnsCorrectRanking()
    {
        var session = CreateSession();
        session.AddStudent("s1");
        session.AddStudent("s2");
        session.AddStudent("s3");

        // Manually set scores via game mode voting
        var gameSession = new Session.Builder()
            .WithQuiz(session.Quiz)
            .WithGameMode(true)
            .WithGameTimerSeconds(10)
            .Build();

        gameSession.AddStudent("s1");
        gameSession.AddStudent("s2");

        gameSession.RecordVote("s1", 0, "A"); // correct
        gameSession.RecordVote("s2", 0, "B"); // wrong

        var (rank, total) = gameSession.GetStudentRank("s1");
        Assert.Equal(1, rank);
        Assert.Equal(2, total);
    }
}
