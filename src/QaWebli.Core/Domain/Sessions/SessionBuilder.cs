using System;
using System.Collections.Generic;
using QaWebli.Core.Domain.Questions;

namespace QaWebli.Core.Domain.Sessions;

public class SessionBuilder
{
    private Session _session = new Session();

    public SessionBuilder SetPresenter(string presenterId)
    {
        _session.PresenterId = presenterId;
        return this;
    }

    public SessionBuilder GenerateJoinCode(string prefix = "QA")
    {
        string randomHex = Guid.NewGuid().ToString("X").Substring(0, 4);
        _session.JoinCode = $"{prefix}-{randomHex}";
        return this;
    }

    public SessionBuilder Activate()
    {
        _session.IsActive = true;
        _session.CreatedAt = DateTime.UtcNow;
        return this;
    }

    public Session Build()
    {
        Session result = _session;
        _session = new Session(); // Reset for reuse
        return result;
    }
}