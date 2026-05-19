// Reminder: this interface is for anything that wants to listen to session changes.
// The session sends events here instead of talking directly to the UI or other parts.
using QaWebli.Domain.Events;

namespace QaWebli.Application.Interfaces;

// Reminder: this is the contract for session listeners.
public interface ISessionObserver
{
    // Reminder: used when the current question changes.
    Task OnQuestionChangedAsync(QuestionChangedEvent e);

    // Reminder: used when a vote is received.
    Task OnVoteReceivedAsync(VoteReceivedEvent e);

    // Reminder: used when a student's presence changes.
    Task OnStudentPresenceChangedAsync(StudentPresenceEvent e);

    // Reminder: used when the instructor reveals the answer.
    Task OnAnswerRevealedAsync(AnswerRevealedEvent e);

    // Reminder: used when the game is finished/completed.
    Task OnGameFinishedAsync(GameFinishedEvent e);
}