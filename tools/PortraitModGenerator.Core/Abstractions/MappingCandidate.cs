namespace PortraitModGenerator.Core.Abstractions;

public sealed class MappingCandidate
{
    public required string SourceAbsolutePath { get; init; }

    public required string RelativePath { get; init; }

    public required string FileName { get; init; }

    public required bool Selected { get; init; }

    public required bool Ignored { get; init; }

    public string? IgnoredReason { get; init; }

    public string? MatchedCardId { get; init; }

    public string? CanonicalName { get; init; }

    public string? Group { get; init; }

    public double Confidence { get; init; }

    public string? MatchReason { get; init; }
}
