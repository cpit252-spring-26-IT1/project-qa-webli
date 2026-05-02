namespace QaWebli.Domain.Entities;
public sealed class Question
{
    public int Number { get; }
    public string RawText { get; }
    public IReadOnlyList<Option> Options { get; }

    public Option? CorrectOption => Options.FirstOrDefault(o => o.IsCorrect);

    private Question(int number, string rawText, IReadOnlyList<Option> options)
    {
        Number = number;
        RawText = rawText;
        Options = options;
    }

    // ── Builder ────────────────────────────────────────────────────────────
    public sealed class Builder
    {
        private int _number;
        private string? _rawText;
        private readonly List<Option> _options = [];

        public Builder WithNumber(int number) { _number = number; return this; }

        public Builder WithRawText(string text) { _rawText = text; return this; }

        public Builder AddOption(string label, string text, bool isCorrect = false)
        {
            _options.Add(new Option(label, text, isCorrect));
            return this;
        }

        public Question Build()
        {
            if (string.IsNullOrWhiteSpace(_rawText))
                throw new InvalidOperationException("Question must have text.");
            if (_options.Count == 0)
                throw new InvalidOperationException($"Question {_number} must have at least one option.");

            return new Question(_number, _rawText!, _options.AsReadOnly());
        }
    }
}
