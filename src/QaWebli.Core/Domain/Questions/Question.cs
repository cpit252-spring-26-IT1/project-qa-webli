using System;
using System.Collections.Generic;

namespace QaWebli.Core.Domain.Questions;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MarkdownText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<Option> Options { get; set; } = new();
}