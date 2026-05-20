// AI-Generated Test Suite
using QaWebli.Domain.Entities;

namespace QaWebli.Tests;

public class SessionScoringTests
{
    private static Quiz CreateSampleQuiz()
    {
        var q1 = new Question.Builder()
            .WithNumber(1)
            .WithRawText("What is 2+2?")
            .AddOption("A", "3")
            .AddOption("B", "4", isCorrect: true)
            .AddOption("C", "5")
            .Build();

        var q2 = new Question.Builder()
            .WithNumber(2)
            .WithRawText("What is 3+3?")
            .AddOption("A", "5")
            .AddOption("B", "6", isCorrect: true)
            .Build();

        return new Quiz.Builder()
            .WithTitle("Math Test")
            .AddQuestion(q1)
            .AddQuestion(q2)
            .Build();
    }

    [Fact]
    public void RecordVote_NewVote_ReturnsTrue()
    {
        var session = new Session.Builder()
            .WithQuiz(CreateSampleQuiz())
            .Build();

        bool isNew = session.RecordVote("student1", 0, "A");

        Assert.True(isNew);
        Assert.Equal(1, session.TotalVotesCast);
    }

    [Fact]
    public void RecordVote_DuplicateVote_ReturnsFalse()
    {
        var session = new Session.Builder()
            .WithQuiz(CreateSampleQuiz())
            .Build();

        session.RecordVote("student1", 0, "A");
        bool isNew = session.RecordVote("student1", 0, "B");

        Assert.False(isNew);
        Assert.Equal(1, session.TotalVotesCast); // still 1, not 2
    }

    [Fact]
    public void RecordVote_GameMode_CorrectAnswer_AwardsPoints()
    {
        var session = new Session.Builder()
            .WithQuiz(CreateSampleQuiz())
            .WithGameMode(true)
            .WithGameTimerSeconds(10)
            .Build();

        // Vote immediately — should get near-max points
        session.RecordVote("student1", 0, "B"); // B is correct

        int points = session.GetPointsEarned("student1", 0);
        Assert.True(points > 0, "Correct answer should earn points");
    }

    [Fact]
    public void RecordVote_GameMode_WrongAnswer_ZeroPoints()
    {
        var session = new Session.Builder()
            .WithQuiz(CreateSampleQuiz())
            .WithGameMode(true)
            .WithGameTimerSeconds(10)
            .Build();

        session.RecordVote("student1", 0, "A"); // A is wrong

        int points = session.GetPointsEarned("student1", 0);
        Assert.Equal(0, points);
    }

    [Fact]
    public void RecordVote_NormalMode_NeverAwardsPoints()
    {
        var session = new Session.Builder()
            .WithQuiz(CreateSampleQuiz())
            .WithGameMode(false)
            .Build();

        session.RecordVote("student1", 0, "B"); // B is correct

        int points = session.GetPointsEarned("student1", 0);
        Assert.Equal(0, points); // no scoring in normal mode
    }

    [Fact]
    public void GetVoteCounts_ReturnsCorrectDistribution()
    {
        var session = new Session.Builder()
            .WithQuiz(CreateSampleQuiz())
            .Build();

        session.RecordVote("s1", 0, "A");
        session.RecordVote("s2", 0, "B");
        session.RecordVote("s3", 0, "B");

        var counts = session.GetVoteCounts(0);

        Assert.Equal(1, counts["A"]);
        Assert.Equal(2, counts["B"]);
        Assert.Equal(0, counts["C"]);
    }
}
