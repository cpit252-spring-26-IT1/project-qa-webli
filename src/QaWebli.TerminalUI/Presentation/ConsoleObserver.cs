using System;
using System.Threading.Tasks;
using QaWebli.Application.Interfaces;
using QaWebli.Domain.Events;

namespace QaWebli.TerminalUI.Presentation;

/// <summary>
/// Observer Pattern: Acts as a subscriber in the Presentation layer.
/// Listens for domain events (e.g., votes, student joins) and triggers
/// a terminal UI re-render so the instructor sees live updates.
/// </summary>
public class ConsoleObserver : ISessionObserver
{
    private readonly Action _onRenderNeeded;

    public ConsoleObserver(Action onRenderNeeded)
    {
        _onRenderNeeded = onRenderNeeded;
    }

    public Task OnQuestionChangedAsync(QuestionChangedEvent e)
    {
        Console.WriteLine($"  [Event] Question changed to {e.QuestionIndex + 1}/{e.TotalQuestions}");
        return Task.CompletedTask;
    }

    public Task OnVoteReceivedAsync(VoteReceivedEvent e)
    {
        _onRenderNeeded();
        return Task.CompletedTask;
    }

    public Task OnStudentPresenceChangedAsync(StudentPresenceEvent e)
    {
        _onRenderNeeded();
        return Task.CompletedTask;
    }
}
