namespace PortraitModGenerator.Core.Abstractions;

public sealed class MergedReviewSession
{
    public string SessionId { get; set; } = string.Empty;

    public string SessionRoot { get; set; } = string.Empty;

    public string OfficialCardIndexPath { get; set; } = string.Empty;

    public string OutputJsonPath { get; set; } = string.Empty;

    public List<ImportedPackage> Packages { get; set; } = [];

    public List<MergedMappingCandidate> Candidates { get; set; } = [];

    public List<CardConflictGroup> ConflictGroups { get; set; } = [];

    public List<ResolvedMapping> ResolvedMappings { get; set; } = [];

    public int TotalAssets { get; set; }

    public int MatchedAssets { get; set; }

    public int UnmatchedAssets { get; set; }

    public int IgnoredAssets { get; set; }

    public int PendingAssets { get; set; }

    public int ResolvedAssets { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
