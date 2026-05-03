using System.Text.RegularExpressions;
using QaWebli.Domain.ValueObjects;

namespace QaWebli.Infrastructure.Parsing;

public static partial class ContentBlockParser
{
    [GeneratedRegex(@"^```(\w*)$", RegexOptions.Multiline)]
    private static partial Regex FenceOpen();

    [GeneratedRegex(@"^```$", RegexOptions.Multiline)]
    private static partial Regex FenceClose();

    public static IReadOnlyList<ContentBlock> Parse(string rawText)
    {
        var result = new List<ContentBlock>();
        var lines = rawText.Split('\n');

        var plainLines = new List<string>();
        var codeLines = new List<string>();
        string? currentLang = null;
        bool inFence = false;

        foreach (var line in lines)
        {
            if (!inFence)
            {
                var openMatch = FenceOpen().Match(line.TrimEnd());
                if (openMatch.Success)
                {
                    FlushPlain(result, plainLines);
                    currentLang = openMatch.Groups[1].Value;
                    inFence = true;
                    continue;
                }
                plainLines.Add(line);
            }
            else
            {
                if (FenceClose().IsMatch(line.TrimEnd()))
                {
                    var code = string.Join('\n', codeLines).TrimEnd();
                    result.Add(new ContentBlock.CodeBlock(currentLang ?? string.Empty, code));
                    codeLines.Clear();
                    currentLang = null;
                    inFence = false;
                    continue;
                }
                codeLines.Add(line);
            }
        }

        if (inFence && codeLines.Count > 0)
            result.Add(new ContentBlock.CodeBlock(currentLang ?? string.Empty, string.Join('\n', codeLines).TrimEnd()));
        else
            FlushPlain(result, plainLines);

        return result.AsReadOnly();
    }

    private static void FlushPlain(List<ContentBlock> result, List<string> lines)
    {
        var text = string.Join('\n', lines).Trim();
        if (!string.IsNullOrEmpty(text))
            result.Add(new ContentBlock.PlainText(text));
        lines.Clear();
    }
}
