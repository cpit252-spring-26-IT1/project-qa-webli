using System.Collections.Concurrent;

namespace QaWebli.Domain.Entities;

public sealed class Session
{
    public string Id { get; }
    public Quiz Quiz { get; }
    public int CurrentQuestionIndex { get; private set; }
    public Question CurrentQuestion => Quiz.Questions[CurrentQuestionIndex];

    private readonly ConcurrentDictionary<string, bool> _connectedStudents = new();

    public int StudentCount => _connectedStudents.Count;

    private Session(string id, Quiz quiz, int startIndex)
    {
        Id = id;
        Quiz = quiz;
        CurrentQuestionIndex = startIndex;
        for (int i = 0; i < quiz.Questions.Count; i++)
            _votes[i] = new ConcurrentDictionary<string, string>();
    }

    public bool TryMoveNext(out int newIndex)
    {
        if (CurrentQuestionIndex >= Quiz.TotalQuestions - 1) { newIndex = CurrentQuestionIndex; return false; }
        newIndex = ++CurrentQuestionIndex;
        return true;
    }

    public bool TryMovePrevious(out int newIndex)
    {
        if (CurrentQuestionIndex <= 0) { newIndex = CurrentQuestionIndex; return false; }
        newIndex = --CurrentQuestionIndex;
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

    // ── Voting ─────────────────────────────────────────────────────────

    // studentId → optionLabel voted on the indexed question
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, string>> _votes = new();
    private int _totalVotesCast;

    public int TotalVotesCast => _totalVotesCast;
    public int CurrentQuestionVotesCast => _votes.TryGetValue(CurrentQuestionIndex, out var qv) ? qv.Count : 0;

    /// Records a vote. Returns true if this is a new vote, false if a vote change.</summary>
    public bool RecordVote(string studentId, int questionIndex, string option)
    {
        var questionVotes = _votes.GetOrAdd(questionIndex, _ => new ConcurrentDictionary<string, string>());
        bool isNew = !questionVotes.ContainsKey(studentId);
        questionVotes[studentId] = option;
        if (isNew) Interlocked.Increment(ref _totalVotesCast);
        return isNew;
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

        public Builder WithId(string id) { _id = id; return this; }
        public Builder WithQuiz(Quiz quiz) { _quiz = quiz; return this; }
        public Builder StartingAtQuestion(int index) { _startIndex = index; return this; }

        public Session Build()
        {
            if (_quiz is null)
                throw new InvalidOperationException("Session requires a Quiz.");
            if (_startIndex < 0 || _startIndex >= _quiz.TotalQuestions)
                throw new InvalidOperationException($"Start index {_startIndex} is out of range.");

            return new Session(_id, _quiz, _startIndex);
        }
    }
}