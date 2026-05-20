using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace QaWebli.Domain.Entities;

public sealed class Session
{
    public string Id { get; }
    public Quiz Quiz { get; }
    public int CurrentQuestionIndex { get; private set; }
    public Question CurrentQuestion => Quiz.Questions[CurrentQuestionIndex];

    public bool IsGameMode { get; }
    public int GameTimerSeconds { get; }
    
    private bool _isLobbyActive = true;
    public bool IsLobbyActive
    {
        get => IsGameMode && _isLobbyActive;
        set => _isLobbyActive = value;
    }
    public ConcurrentDictionary<string, int> StudentScores { get; } = new();
    public ConcurrentDictionary<string, string> DisplayNames { get; } = new();

    public void SetDisplayName(string studentId, string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(trimmed))
            DisplayNames[studentId] = trimmed;
    }

    public string GetDisplayName(string studentId)
        => DisplayNames.TryGetValue(studentId, out var name) ? name : studentId;

    private readonly ConcurrentDictionary<string, bool> _connectedStudents = new();
    private readonly ConcurrentDictionary<int, DateTime> _questionStartTimes = new();

    public int StudentCount => _connectedStudents.Count;

    private Session(string id, Quiz quiz, int startIndex, bool isGameMode, int gameTimerSeconds)
    {
        Id = id;
        Quiz = quiz;
        CurrentQuestionIndex = startIndex;
        IsGameMode = isGameMode;
        GameTimerSeconds = gameTimerSeconds;
        _questionStartTimes[startIndex] = DateTime.UtcNow;

        for (int i = 0; i < quiz.Questions.Count; i++)
            _votes[i] = new ConcurrentDictionary<string, string>();
    }

    public void SetQuestionStartTime(int index, DateTime time)
    {
        _questionStartTimes[index] = time;
    }

    public DateTime GetQuestionStartTime(int index)
    {
        return _questionStartTimes.TryGetValue(index, out var t) ? t : DateTime.UtcNow;
    }

    public bool TryMoveNext(out int newIndex)
    {
        if (CurrentQuestionIndex >= Quiz.TotalQuestions - 1) { newIndex = CurrentQuestionIndex; return false; }
        newIndex = ++CurrentQuestionIndex;
        _questionStartTimes[newIndex] = DateTime.UtcNow;
        return true;
    }

    public bool TryMovePrevious(out int newIndex)
    {
        if (CurrentQuestionIndex <= 0) { newIndex = CurrentQuestionIndex; return false; }
        newIndex = --CurrentQuestionIndex;
        _questionStartTimes[newIndex] = DateTime.UtcNow;
        return true;
    }

    public void AddStudent(string studentId) => _connectedStudents[studentId] = true;
    public void RemoveStudent(string studentId) => _connectedStudents.TryRemove(studentId, out _);

    // ── Answer Reveal ──────────────────────────────────────────────────
    private readonly ConcurrentDictionary<int, bool> _revealedAnswers = new();

    public bool IsAnswerRevealed(int questionIndex)
    {
        return _revealedAnswers.TryGetValue(questionIndex, out var revealed) && revealed;
    }

    public bool RevealAnswer(int questionIndex)
    {
        var question = Quiz.Questions[questionIndex];
        if (question.CorrectOption == null)
            return false;

        return _revealedAnswers.TryAdd(questionIndex, true);
    }

    // ── Votes ──────────────────────────────────────────────────────────
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, string>> _votes = new();
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, int>> _questionPoints = new();
    private int _totalVotesCast;

    public int TotalVotesCast => _totalVotesCast;
    public int CurrentQuestionVotesCast => _votes.TryGetValue(CurrentQuestionIndex, out var qv) ? qv.Count : 0;

    //Records a vote. Returns true if this is a new vote, false if a vote change.
    public bool RecordVote(string studentId, int questionIndex, string option)
    {
        var questionVotes = _votes.GetOrAdd(questionIndex, _ => new ConcurrentDictionary<string, string>());
        bool isNew = !questionVotes.ContainsKey(studentId);
        questionVotes[studentId] = option;
        if (isNew) Interlocked.Increment(ref _totalVotesCast);

        if (IsGameMode && isNew)
        {
            var question = Quiz.Questions[questionIndex];
            var startTime = GetQuestionStartTime(questionIndex);
            var elapsed = DateTime.UtcNow - startTime;
            var elapsedCentiseconds = (int)(elapsed.TotalMilliseconds / 10);
            var maxCentiseconds = GameTimerSeconds * 100;

            if (elapsedCentiseconds <= maxCentiseconds)
            {
                bool isCorrect = question.CorrectOption?.Label == option;
                if (isCorrect)
                {
                    int points = Math.Max(0, maxCentiseconds - Math.Max(0, elapsedCentiseconds));
                    if (points > 0)
                    {
                        var qPoints = _questionPoints.GetOrAdd(questionIndex, _ => new ConcurrentDictionary<string, int>());
                        qPoints[studentId] = points;

                        StudentScores.AddOrUpdate(studentId, points, (_, current) => current + points);
                    }
                }
            }
        }

        return isNew;
    }

    public int GetPointsEarned(string studentId, int questionIndex)
    {
        if (_questionPoints.TryGetValue(questionIndex, out var qp))
        {
            if (qp.TryGetValue(studentId, out var points))
            {
                return points;
            }
        }
        return 0;
    }

    public (int rank, int total) GetStudentRank(string studentId)
    {
        var list = _connectedStudents.Keys
            .Select(id => new KeyValuePair<string, int>(id, StudentScores.GetValueOrDefault(id, 0)))
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key)
            .ToList();

        var index = list.FindIndex(kv => kv.Key == studentId);
        if (index == -1)
        {
            return (list.Count + 1, list.Count + 1);
        }
        return (index + 1, list.Count);
    }

    public Dictionary<string, int> GetVoteCounts(int questionIndex)
    {
        var question = Quiz.Questions[questionIndex];
        var result = question.Options.ToDictionary(o => o.Label, _ => 0);
        if (_votes.TryGetValue(questionIndex, out var qv))
            foreach (var vote in qv.Values)
                if (result.ContainsKey(vote))
                    result[vote]++;
        return result;
    }

    // ── Builder ─────────────────────────────────────────────────────────
    public sealed class Builder
    {
        private string _id = Guid.NewGuid().ToString("N")[..8];
        private Quiz? _quiz;
        private int _startIndex;
        private bool _isGameMode;
        private int _gameTimerSeconds = 10;

        public Builder WithId(string id) { _id = id; return this; }
        public Builder WithQuiz(Quiz quiz) { _quiz = quiz; return this; }
        public Builder StartingAtQuestion(int index) { _startIndex = index; return this; }
        public Builder WithGameMode(bool isGameMode) { _isGameMode = isGameMode; return this; }
        public Builder WithGameTimerSeconds(int seconds) { _gameTimerSeconds = seconds; return this; }

        public Session Build()
        {
            if (_quiz is null)
                throw new InvalidOperationException("Session requires a Quiz.");
            if (_startIndex < 0 || _startIndex >= _quiz.TotalQuestions)
                throw new InvalidOperationException($"Start index {_startIndex} is out of range.");

            return new Session(_id, _quiz, _startIndex, _isGameMode, _gameTimerSeconds);
        }
    }
}