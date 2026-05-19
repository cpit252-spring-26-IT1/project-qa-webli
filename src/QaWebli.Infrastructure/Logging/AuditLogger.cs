using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QaWebli.Application.Interfaces;
using QaWebli.Domain.Entities;
using QaWebli.Domain.Events;

namespace QaWebli.Infrastructure.Logging;

/// <summary>
/// Observes session state events and writes them to a log file.
/// Supports both standard QA sessions and game-specific logging.
/// </summary>
public sealed class AuditLogger : ISessionObserver, IDisposable
{
    private static AuditLogger? _instance;
    
    public static AuditLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException("AuditLogger must be initialized with a Session using Initialize(Session).");
            }
            return _instance;
        }
    }

    public static void Initialize(Session session)
    {
        _instance = new AuditLogger(session);
    }

    private readonly object _lock = new();
    private readonly Session _session;
    private readonly StreamWriter _writer;
    private readonly string _logFilePath;

    private AuditLogger(Session session)
    {
        _session = session;

        // Ensure `logs/` exists at the solution root.
        var logDirectory = Path.Combine(ResolveSolutionRoot(), "logs");
        Directory.CreateDirectory(logDirectory);

        // Standard Utc timestamping and qa-session prefix for initial commit.
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        _logFilePath = Path.Combine(logDirectory, $"qa-session-{timestamp}.log");
        _writer = new StreamWriter(_logFilePath, append: true)
        {
            AutoFlush = true
        };
    }

    private static string ResolveSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QaWebli.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }

    public void LogSessionStart(string sessionId, string quizTitle)
    {
        lock (_lock)
        {
            var header = _session.IsGameMode ? "GAME_START" : "SESSION_START";
            _writer.WriteLine($"[{DateTime.UtcNow:O}] {header} | {sessionId} | {quizTitle}");
        }
    }

    public void LogSessionEnd(string sessionId)
    {
        lock (_lock)
        {
            var footer = _session.IsGameMode ? "GAME_END" : "SESSION_END";
            _writer.WriteLine($"[{DateTime.UtcNow:O}] {footer} | {sessionId}");
        }
    }

    // ── ISessionObserver ──────────────────────────────────────────────────

    public Task OnQuestionChangedAsync(QuestionChangedEvent e)
    {
        lock (_lock)
        {
            _writer.WriteLine($"[{DateTime.UtcNow:O}] QUESTION_CHANGED | {e.QuestionIndex + 1}/{e.TotalQuestions}");
        }
        return Task.CompletedTask;
    }

    public Task OnVoteReceivedAsync(VoteReceivedEvent e)
    {
        lock (_lock)
        {
            if (_session.IsGameMode)
            {
                var startTime = _session.GetQuestionStartTime(e.QuestionIndex);
                var elapsed = e.Timestamp - startTime;
                var centiseconds = (int)(elapsed.TotalMilliseconds / 10.0);

                var correctOpt = _session.CurrentQuestion.CorrectOption;
                var isCorrect = correctOpt != null && correctOpt.Label.Equals(e.OptionLabel, StringComparison.OrdinalIgnoreCase);
                var pointsEarned = _session.GetPointsEarned(e.StudentId, e.QuestionIndex);

                _writer.WriteLine($"[{DateTime.UtcNow:O}] GAME_VOTE | {e.StudentId} | Q{e.QuestionIndex + 1} | Option: {e.OptionLabel} | Correct: {isCorrect} | ResponseTime: {centiseconds}cs | PointsEarned: {pointsEarned}");
            }
            else
            {
                _writer.WriteLine($"[{DateTime.UtcNow:O}] STUDENT_VOTED | {e.StudentId} | Q{e.QuestionIndex + 1} | Option: {e.OptionLabel}");
            }
        }
        return Task.CompletedTask;
    }

    public Task OnStudentPresenceChangedAsync(StudentPresenceEvent e)
    {
        lock (_lock)
        {
            var action = e.Joined ? "JOINED" : "LEFT";
            var prefix = _session.IsGameMode ? "GAME" : "STUDENT";
            _writer.WriteLine($"[{DateTime.UtcNow:O}] {prefix}_{action} | {e.StudentId}");
        }
        return Task.CompletedTask;
    }

    public Task OnAnswerRevealedAsync(AnswerRevealedEvent e)
    {
        lock (_lock)
        {
            _writer.WriteLine($"[{DateTime.UtcNow:O}] ANSWER_REVEALED | Q{e.QuestionIndex + 1} | Correct Option: {e.CorrectOptionLabel}");
        }
        return Task.CompletedTask;
    }

    public Task OnGameFinishedAsync(GameFinishedEvent e)
    {
        lock (_lock)
        {
            _writer.WriteLine($"[{DateTime.UtcNow:O}] GAME_FINISHED");
            var sorted = e.Leaderboard.OrderByDescending(kv => kv.Value).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                _writer.WriteLine($"[{DateTime.UtcNow:O}] LEADERBOARD_RANK | Rank {i + 1} | {sorted[i].Key} | Score: {sorted[i].Value}");
            }
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }
}