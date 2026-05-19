namespace QaWebli.Domain.Events;

// Questions navigation.

public sealed record QuestionChangedEvent(int QuestionIndex, int TotalQuestions, DateTime Timestamp)
{
    public static QuestionChangedEvent Now(int index, int total) =>
        new(index, total, DateTime.UtcNow);
}

// When a student votes for an option or edits it.

public sealed record VoteReceivedEvent(int QuestionIndex, string StudentId, string OptionLabel, DateTime Timestamp)
{
    public static VoteReceivedEvent Now(int questionIndex, string studentId, string option) =>
        new(questionIndex, studentId, option, DateTime.UtcNow);
}

// Student joins or leaves the session.

public sealed record StudentPresenceEvent(int QuestionIndex, string StudentId, bool Joined, DateTime Timestamp)
{
    public static StudentPresenceEvent Now(int questionIndex, string studentId, bool joined) =>
        new(questionIndex, studentId, joined, DateTime.UtcNow);
}

// Instructor reveals the correct answer.

public sealed record AnswerRevealedEvent(int QuestionIndex, string CorrectOptionLabel, DateTime Timestamp)
{
    public static AnswerRevealedEvent Now(int index, string correctOptionLabel) =>
        new(index, correctOptionLabel, DateTime.UtcNow);
}

// Game over/finished.
public sealed record GameFinishedEvent(Dictionary<string, int> Leaderboard, DateTime Timestamp)
{
    public static GameFinishedEvent Now(Dictionary<string, int> leaderboard) =>
        new(leaderboard, DateTime.UtcNow);
}