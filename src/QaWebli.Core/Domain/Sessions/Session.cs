using System;
using System.Collections.Generic;
using QaWebli.Core.Domain.Questions;

namespace QaWebli.Core.Domain.Sessions;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JoinCode { get; set; } = string.Empty;
    public string PresenterId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public List<Question> Questions { get; set; } = new();
}