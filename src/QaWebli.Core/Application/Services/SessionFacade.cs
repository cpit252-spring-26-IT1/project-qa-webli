// Reminder: this facade keeps the session flow in one place.
// It also handles notifying observers when the question changes.
using QaWebli.Application.Interfaces;
using QaWebli.Domain.Entities;
using QaWebli.Domain.Events;

namespace QaWebli.Application.Services;

// Reminder: this class is the middle point between the session and its observers.
public class SessionFacade
{
    // Reminder: this is the actual session being managed.
    private readonly Session _session;

    // Reminder: this stores the listeners that should get updates.
    private readonly List<ISessionObserver> _observers = [];

    // Reminder: outside code can read the session but should not change it directly.
    public Session Session => _session;

    // Reminder: create the facade with the session we want to manage.
    public SessionFacade(Session session)
    {
        _session = session;
    }

    // Reminder: add a listener so it gets future updates.
    public void Subscribe(ISessionObserver observer) => _observers.Add(observer);

    // Reminder: move to the next question and tell everyone if it worked.
    public async Task NextQuestionAsync()
    {
        // Reminder: MoveNext() only returns true if there is another question.
        if (_session.MoveNext())
            // Reminder: send the new question number to every observer.
            await PublishAsync(obs => obs.OnQuestionChangedAsync(
                QuestionChangedEvent.Now(_session.CurrentQuestionIndex, _session.Quiz.TotalQuestions)));
    }

    // Reminder: move to the previous question and tell everyone if it worked.
    public async Task PreviousQuestionAsync()
    {
        // Reminder: MovePrevious() only returns true if there is an earlier question.
        if (_session.MovePrevious())
            // Reminder: send the new question number to every observer.
            await PublishAsync(obs => obs.OnQuestionChangedAsync(
                QuestionChangedEvent.Now(_session.CurrentQuestionIndex, _session.Quiz.TotalQuestions)));
    }

    // Reminder: run the same action for every observer and wait for all of them.
    private async Task PublishAsync(Func<ISessionObserver, Task> action)
    {
        // Reminder: each observer may do async work, so wait for all of them.
        await Task.WhenAll(_observers.Select(action));
    }
}