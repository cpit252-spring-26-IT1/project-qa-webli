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
        var mathLines = new List<string>();
        string? currentLang = null;
        bool inFence = false;
        bool inMath = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd();
            var fullyTrimmed = trimmedLine.Trim();

            if (inFence)
            {
                if (FenceClose().IsMatch(trimmedLine))
                {
                    var code = string.Join('\n', codeLines).TrimEnd();
                    result.Add(new ContentBlock.CodeBlock(currentLang ?? string.Empty, code));
                    codeLines.Clear();
                    currentLang = null;
                    inFence = false;
                }
                else
                {
                    codeLines.Add(line);
                }
            }
            else if (inMath)
            {
                if (fullyTrimmed == "$$")
                {
                    var expr = string.Join('\n', mathLines).Trim();
                    result.Add(new ContentBlock.MathBlock(expr));
                    mathLines.Clear();
                    inMath = false;
                }
                else
                {
                    mathLines.Add(line);
                }
            }
            else
            {
                var openMatch = FenceOpen().Match(trimmedLine);
                if (openMatch.Success)
                {
                    FlushPlain(result, plainLines);
                    currentLang = openMatch.Groups[1].Value;
                    inFence = true;
                }
                else if (fullyTrimmed.StartsWith("$$") && fullyTrimmed.EndsWith("$$") && fullyTrimmed.Length > 2)
                {
                    FlushPlain(result, plainLines);
                    var expr = fullyTrimmed[2..^2].Trim();
                    result.Add(new ContentBlock.MathBlock(expr));
                }
                else if (fullyTrimmed == "$$")
                {
                    FlushPlain(result, plainLines);
                    inMath = true;
                }
                else
                {
                    plainLines.Add(line);
                }
            }
        }

        if (inFence && codeLines.Count > 0)
        {
            result.Add(new ContentBlock.CodeBlock(currentLang ?? string.Empty, string.Join('\n', codeLines).TrimEnd()));
        }
        else if (inMath && mathLines.Count > 0)
        {
            result.Add(new ContentBlock.MathBlock(string.Join('\n', mathLines).Trim()));
        }
        else
        {
            FlushPlain(result, plainLines);
        }

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
