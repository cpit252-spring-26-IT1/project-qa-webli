namespace QaWebli.Domain.Entities;


public sealed class Session
{
    public string Id { get; }
    public Quiz Quiz { get; }
    public int CurrentQuestionIndex { get; private set; }
    public Question CurrentQuestion => Quiz.Questions[CurrentQuestionIndex];

    private Session(string id, Quiz quiz, int startIndex)
    {
        Id = id;
        Quiz = quiz;
        CurrentQuestionIndex = startIndex;
    }

    public bool MoveNext()
    {
        if (CurrentQuestionIndex >= Quiz.TotalQuestions - 1) return false;
        CurrentQuestionIndex++;
        return true;
    }

    public bool MovePrevious()
    {
        if (CurrentQuestionIndex <= 0) return false;
        CurrentQuestionIndex--;
        return true;
    }

    // ── Builder ────────────────────────────────────────────────────────────
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