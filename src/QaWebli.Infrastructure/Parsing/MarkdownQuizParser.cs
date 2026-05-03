using QaWebli.Domain.Entities;
using System.Text.RegularExpressions;

namespace QaWebli.Infrastructure.Parsing;

/// <summary>
/// Reads a Markdown quiz file and constructs a Quiz aggregate.
/// 
/// NOTE: The regex-based parsing logic below was generated with AI assistance.
/// The educational focus of this project is on design patterns and domain modeling,
/// not on hand-writing Markdown parsers.
/// </summary>
public static class MarkdownQuizParser
{
    private static readonly Regex TitleRegex = new(@"^#\s+(.+)$", RegexOptions.Multiline);
    /// <summary>Matches <c>## 1. Prompt</c> or <c>## Question 1: Prompt</c>; prompt may span lines until a checklist option (<c>- [ ]</c>).</summary>
    private static readonly Regex QuestionRegex = new(
        @"^##\s+(?:Question\s+)?(\d+)[.:]\s+([\s\S]+?)(?=\n(?:\r?\n)*\s*-\s*\[|\z)",
        RegexOptions.Multiline);
    private static readonly Regex OptionRegex = new(@"^-\s+\[([ xX])\]\s+(.+)$", RegexOptions.Multiline);

    public static Quiz ParseFile(string path)
    {
        var text = File.ReadAllText(path);
        return Parse(text);
    }

    public static Quiz Parse(string text)
    {
        var titleMatch = TitleRegex.Match(text);
        if (!titleMatch.Success)
            throw new FormatException("Quiz must start with a # title.");

        var builder = new Quiz.Builder().WithTitle(titleMatch.Groups[1].Value.Trim());

        var questionMatches = QuestionRegex.Matches(text);
        foreach (Match qm in questionMatches)
        {
            int number = int.Parse(qm.Groups[1].Value);
            string rawText = qm.Groups[2].Value.Trim();

            var qb = new Question.Builder()
                .WithNumber(number)
                .WithRawText(rawText);

            // Parse raw text into ContentBlocks (PlainText / CodeBlock)
            var content = ContentBlockParser.Parse(rawText);
            foreach (var block in content)
                qb.AddContentBlock(block);

            var searchStart = qm.Index + qm.Length;
            var nextQuestion = questionMatches.Cast<Match>()
                .FirstOrDefault(m => m.Index > qm.Index);
            var searchEnd = nextQuestion?.Index ?? text.Length;
            var section = text.Substring(searchStart, searchEnd - searchStart);

            foreach (Match om in OptionRegex.Matches(section))
            {
                bool isCorrect = om.Groups[1].Value.Trim().ToLower() == "x";
                string optionText = om.Groups[2].Value.Trim();

                var label = optionText.Length > 0 ? optionText[0].ToString() : "?";
                var body = optionText.Length > 2 ? optionText[3..] : optionText;

                qb.AddOption(label, body, isCorrect);
            }

            builder.AddQuestion(qb.Build());
        }

        return builder.Build();
    }
}