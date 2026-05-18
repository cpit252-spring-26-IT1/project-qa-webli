using QaWebli.Application.Interfaces;
using QaWebli.Domain.Events;

namespace QaWebli.Infrastructure.Logging;

public sealed class AuditLogger : ISessionObserver, IDisposable
{
    // Single shared instance so all parts of the app append to one file handle.
    // Keeps writes serialized and avoids multiple log files per process.
    private static readonly Lazy<AuditLogger> _instance = new(() => new AuditLogger());
    public static AuditLogger Instance => _instance.Value;

    private readonly object _lock = new();
    private readonly StreamWriter _writer;
    private readonly string _logFilePath;

    private AuditLogger()
    {
        // Ensure `logs/` exists at the solution root so files are easy to find.
        var logDirectory = Path.Combine(ResolveSolutionRoot(), "logs");
        Directory.CreateDirectory(logDirectory);

        // Timestamped file name keeps sessions separate and sortable.
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        _logFilePath = Path.Combine(logDirectory, $"qa-session-{timestamp}.log");
        _writer = new StreamWriter(_logFilePath, append: true);
        // AutoFlush ensures each WriteLine is written to disk promptly.
        _writer.AutoFlush = true;
    }

    private static string ResolveSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            // Walk up from the runtime base dir to find the solution file.
            // This makes the logger robust when the process cwd is diffrent from repo root.
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
        // Lock to protect the shared StreamWriter from concurrent writes.
        lock (_lock)
        {
            _writer.WriteLine($"[{DateTime.UtcNow:O}] SESSION_START | {sessionId} | {quizTitle}");
        }
    }

    public void LogSessionEnd(string sessionId)
    {
        // Lock to protect the shared StreamWriter from concurrent writes.
        lock (_lock)
        {
            _writer.WriteLine($"[{DateTime.UtcNow:O}] SESSION_END | {sessionId}");
        }
    }

    // ── ISessionObserver ──────────────────────────────────────────────────

    public Task OnQuestionChangedAsync(QuestionChangedEvent e)
    {
        // Small, focused note: events are written quickly; avoid heavy work here.
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
            _writer.WriteLine($"[{DateTime.UtcNow:O}] STUDENT_VOTED | {e.StudentId} | Q{e.QuestionIndex + 1} | Option: {e.OptionLabel}");
        }
        return Task.CompletedTask;
    }

    public Task OnStudentPresenceChangedAsync(StudentPresenceEvent e)
    {
        lock (_lock)
        {
            var action = e.Joined ? "JOINED" : "LEFT";
            _writer.WriteLine($"[{DateTime.UtcNow:O}] STUDENT_{action} | {e.StudentId}");
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

    public void Dispose()
    {
        // Close the file handle so other tools (editors, readers) can access the file.
        _writer?.Dispose();
    }
}