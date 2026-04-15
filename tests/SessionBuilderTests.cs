using Xunit;
using QaWebli.Core.Domain.Sessions;

namespace QaWebli.Tests;

public class SessionBuilderTests
{
    [Fact]
    public void Build_ShouldCreateValidSession()
    {
        // Arrange
        var builder = new SessionBuilder();
        
        // Act
        var session = builder
            .SetPresenter("Osama")
            .GenerateJoinCode("TEST")
            .Activate()
            .Build();

        // Assert
        Assert.Equal("Osama", session.PresenterId);
        Assert.True(session.IsActive);
        Assert.StartsWith("TEST-", session.JoinCode);
        Assert.Equal(9, session.JoinCode.Length);
    }
}