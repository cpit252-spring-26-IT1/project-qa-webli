namespace QaWebli.Domain.ValueObjects;

public abstract record ContentBlock
{
    private ContentBlock() { }

    public sealed record PlainText(string Text) : ContentBlock;
    public sealed record CodeBlock(string Language, string Code) : ContentBlock;
    public sealed record MathBlock(string Expression) : ContentBlock;
}
