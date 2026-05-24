using QaWebli.TerminalUI.Hosting;

namespace QaWebli.Tests;

public class CliOptionsTests
{
    [Fact]
    public void Parse_ShortGameFlag_EnablesGameMode()
    {
        var options = CliOptions.Parse(["quiz.md", "-g"]);

        Assert.NotNull(options);
        Assert.True(options.Game);
        Assert.Equal(10, options.GameTimerSeconds);
    }

    [Fact]
    public void Parse_NgrokFlag_EnablesNgrok()
    {
        var options = CliOptions.Parse(["quiz.md", "--ngrok"]);

        Assert.NotNull(options);
        Assert.True(options.Ngrok);
    }
}
