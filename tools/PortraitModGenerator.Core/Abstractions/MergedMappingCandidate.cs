namespace PortraitModGenerator.Core.Abstractions;

public sealed class MergedMappingCandidate
{
    public string CandidateId { get; set; } = string.Empty;

    public string SourcePackageId { get; set; } = string.Empty;

    public string SourcePackageName { get; set; } = string.Empty;

    public string SourceAbsolutePath { get; set; } = string.Empty;

    public string SourceRelativePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public bool Selected { get; set; }

    public bool Ignored { get; set; }

    public string? IgnoredReason { get; set; }

    public string? MatchedCardId { get; set; }

    public string? CanonicalName { get; set; }

    public string? Group { get; set; }

    public double Confidence { get; set; }

    public string? MatchReason { get; set; }

    public bool IsConflict { get; set; }

    public bool IsAutoSelected { get; set; }
}
