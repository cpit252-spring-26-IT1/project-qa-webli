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

    // Reminder: start the game, end the lobby mode and announce first question.
    public async Task StartGameAsync()
    {
        _session.IsLobbyActive = false;
        _session.SetQuestionStartTime(_session.CurrentQuestionIndex, DateTime.UtcNow);
        await PublishAsync(obs => obs.OnQuestionChangedAsync(
            QuestionChangedEvent.Now(_session.CurrentQuestionIndex, _session.Quiz.TotalQuestions)));
    }

    // Reminder: move to the next question and tell everyone if it worked.
    public async Task NextQuestionAsync()
    {
        // Reminder: TryMoveNext returns false if already at the last question.
        if (_session.TryMoveNext(out _))
            await PublishAsync(obs => obs.OnQuestionChangedAsync(
                QuestionChangedEvent.Now(_session.CurrentQuestionIndex, _session.Quiz.TotalQuestions)));
    }

    // Reminder: move to the previous question and tell everyone if it worked.
    public async Task PreviousQuestionAsync()
    {
        // Reminder: TryMovePrevious returns false if already at the first question.
        if (_session.TryMovePrevious(out _))
            await PublishAsync(obs => obs.OnQuestionChangedAsync(
                QuestionChangedEvent.Now(_session.CurrentQuestionIndex, _session.Quiz.TotalQuestions)));
    }

    // Reminder: register a student joining the session and notify observers.
    public async Task AddStudentAsync(string studentId)
    {
        _session.AddStudent(studentId);
        await PublishAsync(obs => obs.OnStudentPresenceChangedAsync(
            StudentPresenceEvent.Now(_session.StudentCount, studentId, true)));
    }

    // Reminder: remove a student who disconnected and notify observers.
    public async Task RemoveStudentAsync(string studentId)
    {
        _session.RemoveStudent(studentId);
        await PublishAsync(obs => obs.OnStudentPresenceChangedAsync(
            StudentPresenceEvent.Now(_session.StudentCount, studentId, false)));
    }

    // Reminder: record a student's vote and notify observers.
    public async Task RecordVoteAsync(string studentId, string option)
    {
        bool isNew = _session.RecordVote(studentId, _session.CurrentQuestionIndex, option);
        await PublishAsync(obs => obs.OnVoteReceivedAsync(
            VoteReceivedEvent.Now(_session.CurrentQuestionIndex, studentId, option)));   
    }

    // Reminder: reveal the correct answer for the current question if it has one.
    public async Task<bool> RevealAnswerAsync()
    {
        if (_session.RevealAnswer(_session.CurrentQuestionIndex))
        {
            var correctOption = _session.CurrentQuestion.CorrectOption;
            if (correctOption != null)
            {
                await PublishAsync(obs => obs.OnAnswerRevealedAsync(
                    AnswerRevealedEvent.Now(_session.CurrentQuestionIndex, correctOption.Label)));
                return true;
            }
        }
        return false;
    }

    // Reminder: notify observers that the game is finished and pass the leaderboard.
    public async Task FinishGameAsync(Dictionary<string, int> leaderboard)
    {
        await PublishAsync(obs => obs.OnGameFinishedAsync(GameFinishedEvent.Now(leaderboard)));
    }

    // Reminder: run the same action for every observer and wait for all of them.
    private async Task PublishAsync(Func<ISessionObserver, Task> action)
    {
        // Reminder: each observer may do async work, so wait for all of them.
        await Task.WhenAll(_observers.Select(action));
    }
}