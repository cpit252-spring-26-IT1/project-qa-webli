using System;

namespace QaWebli.Core.Domain.Questions;

public class Option
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int VoteCount { get; set; }
}