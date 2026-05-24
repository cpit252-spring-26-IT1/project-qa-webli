using System;

namespace QaWebli.TerminalUI.Presentation;

public static class ArabicHelper
{
    private const char RLM = '\u200F'; // Right-To-Left Mark

    public static bool ContainsArabic(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        foreach (var c in input)
        {
            if (c >= 0x0600 && c <= 0x06FF) return true;
        }
        return false;
    }

    private static readonly BidiReshapeSharp.Reshaper.ArabicReshaper _reshaper = new();

    public static string Reshape(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        if (!ContainsArabic(input))
            return input;

        // 1. Shape the letters (so they connect properly using presentation forms).
        // 2. Keep the logical order (the terminal's native BiDi will reverse it visually).
        // 3. Prepend RLM to force the terminal to treat the base direction as RTL.
        return RLM + _reshaper.Reshape(input);
    }
}
