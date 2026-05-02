namespace QaWebli.Domain.Entities;


public sealed class Quiz
{
    public string Title { get; }
    public IReadOnlyList<Question> Questions { get; }
    public int TotalQuestions => Questions.Count;

    private Quiz(string title, IReadOnlyList<Question> questions)
    {
        Title = title;
        Questions = questions;
    }

    // ── Builder ────────────────────────────────────────────────────────────
    public sealed class Builder
    {
        private string? _title;
        private readonly List<Question> _questions = [];

        public Builder WithTitle(string title) { _title = title; return this; }

        public Builder AddQuestion(Question question) { _questions.Add(question); return this; }

        public Quiz Build()
        {
            if (string.IsNullOrWhiteSpace(_title))
                throw new InvalidOperationException("Quiz must have a title.");
            if (_questions.Count == 0)
                throw new InvalidOperationException("Quiz must have at least one question.");

            return new Quiz(_title!, _questions.AsReadOnly());
        }
    }
}