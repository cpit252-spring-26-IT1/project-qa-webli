namespace QaWebli.Domain.Entities;

/// <summary>
/// Immutable value object representing a single answer option.
/// </summary>
public sealed record Option(string Label, string Text, bool IsCorrect);
