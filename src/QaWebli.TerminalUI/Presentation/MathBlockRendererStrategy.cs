using System.Text.RegularExpressions;
using QaWebli.Domain.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QaWebli.TerminalUI.Presentation;

public sealed class MathBlockRendererStrategy : IContentRendererStrategy
{
    public bool CanRender(ContentBlock block) => block is ContentBlock.MathBlock;

    public IRenderable Render(ContentBlock block)
    {
        var math = (ContentBlock.MathBlock)block;
        var formatted = FormatMathSymbols(math.Expression);

        return new Panel(new Markup($"[cyan]{Markup.Escape(formatted)}[/]"))
        {
            Header = new PanelHeader(" [bold cyan]expression[/] ", Justify.Right),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1),
            Padding = new Padding(1, 0, 1, 0),
        };
    }

    private static string FormatMathSymbols(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // Strip font formatters first: \mathbf{F} -> F, \mathrm{x} -> x, etc.
        text = Regex.Replace(text, @"\\mathbf\{([^{}]+)\}", "$1");
        text = Regex.Replace(text, @"\\mathrm\{([^{}]+)\}", "$1");
        text = Regex.Replace(text, @"\\vec\{([^{}]+)\}", "$1");

        // Format fractions recursively to correctly handle nested braces
        text = FormatFractions(text);

        // Replace math/relational operators and letters
        text = text
            .Replace("\\iiint", "∫∫∫")
            .Replace("\\iint", "∫∫")
            .Replace("\\int", "∫")
            .Replace("\\infty", "∞")
            .Replace("\\to", "→")
            .Replace("\\partial", "∂")
            .Replace("\\nabla", "∇")
            .Replace("\\sqrt", "√")
            .Replace("\\times", "×")
            .Replace("\\cdot", "·")
            .Replace("\\sum", "∑")
            .Replace("\\delta", "δ")
            .Replace("\\alpha", "α")
            .Replace("\\beta", "β")
            .Replace("\\gamma", "γ")
            .Replace("\\theta", "θ")
            .Replace("\\lambda", "λ")
            .Replace("\\mu", "μ")
            .Replace("\\phi", "φ")
            .Replace("\\psi", "ψ")
            .Replace("\\omega", "ω")
            .Replace("\\Delta", "Δ")
            .Replace("\\Sigma", "Σ")
            .Replace("\\Omega", "Ω")
            .Replace("\\lim", "lim")
            .Replace("\\Phi", "Φ")
            .Replace("\\Psi", "Ψ")
            .Replace("\\Theta", "Θ")
            .Replace("\\sigma", "σ")
            .Replace("\\pi", "π")
            .Replace("\\bowtie", "⋈")
            .Replace("\\land", "∧")
            .Replace("\\lor", "∨")
            .Replace("\\pm", "±")
            .Replace("\\neq", "≠")
            .Replace("\\geq", "≥")
            .Replace("\\leq", "≤")
            .Replace("\\rightarrow", "→")
            .Replace("\\mathcal{COUNT}", "COUNT")
            .Replace("\\mathcal{SUM}", "SUM")
            .Replace("\\mathcal{AVG}", "AVG")
            .Replace("\\mathcal{MIN}", "MIN")
            .Replace("\\mathcal{MAX}", "MAX")
            .Replace("\\mathcal{F}", "F")
            .Replace("\\mathcal", "")
            .Replace("\\left(", "(")
            .Replace("\\right)", ")")
            .Replace("\\left[", "[")
            .Replace("\\right]", "]")
            .Replace("\\left\\{", "{")
            .Replace("\\right\\}", "}")
            .Replace("\\,", " ")
            .Replace("\\_", "_")
            .Replace("\\subset", "⊂")
            .Replace("\\subseteq", "⊆")
            .Replace("\\cup", "∪")
            .Replace("\\cap", "∩")
            .Replace("\\setminus", "∖")
            .Replace("\\div", "÷")
            .Replace("\\quad", "   ")
            .Replace("\\{", "{")
            .Replace("\\}", "}")
            .Replace("_{0}", "₀")
            .Replace("_{1}", "₁")
            .Replace("_{2}", "₂")
            .Replace("_{3}", "₃")
            .Replace("_{n}", "ₙ")
            .Replace("_{x}", "ₓ")
            .Replace("_{y}", "ᵧ")
            .Replace("_0", "₀")
            .Replace("_1", "₁")
            .Replace("_2", "₂")
            .Replace("_3", "₃")
            .Replace("_n", "ₙ")
            .Replace("_x", "ₓ")
            .Replace("_y", "ᵧ")
            .Replace("^{2}", "²")
            .Replace("^{3}", "³")
            .Replace("^{x}", "ˣ")
            .Replace("^{n}", "ⁿ")
            .Replace("^{t}", "ᵗ")
            .Replace("^{-}", "⁻")
            .Replace("^2", "²")
            .Replace("^3", "³")
            .Replace("^x", "ˣ")
            .Replace("^n", "ⁿ")
            .Replace("^t", "ᵗ")
            .Replace("$$", "")
            .Replace("$", "");

        // Remove \text{...} wrappers
        text = Regex.Replace(text, @"\\text\{([^{}]+)\}", "$1");

        return text;
    }

    private static string FormatFractions(string text)
    {
        while (true)
        {
            int index = text.IndexOf("\\frac{");
            if (index == -1) break;

            int numStart = index + 6;
            int numEnd = FindMatchingBrace(text, numStart);
            if (numEnd == -1) break;

            string numerator = text.Substring(numStart, numEnd - numStart);

            int denStart = numEnd + 1;
            if (denStart >= text.Length || text[denStart] != '{') break;

            int denEnd = FindMatchingBrace(text, denStart + 1);
            if (denEnd == -1) break;

            string denominator = text.Substring(denStart + 1, denEnd - (denStart + 1));

            // Format nested math symbols recursively
            numerator = FormatMathSymbols(numerator);
            denominator = FormatMathSymbols(denominator);

            if (numerator.Contains(" ") || numerator.Contains("+") || numerator.Contains("-")) 
                numerator = $"({numerator})";
            if (denominator.Contains(" ") || denominator.Contains("+") || denominator.Contains("-")) 
                denominator = $"({denominator})";

            string fractionReplacement = $"{numerator}/{denominator}";
            text = text.Substring(0, index) + fractionReplacement + text.Substring(denEnd + 1);
        }
        return text;
    }

    private static int FindMatchingBrace(string text, int start)
    {
        int depth = 1;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') depth--;

            if (depth == 0) return i;
        }
        return -1;
    }
}
