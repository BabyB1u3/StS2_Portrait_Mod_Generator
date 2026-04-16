namespace PortraitModGenerator.Core.Abstractions;

public sealed class CardConflictGroup
{
    public string CardId { get; set; } = string.Empty;

    public string CanonicalName { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public List<string> CandidateIds { get; set; } = [];

    public string? SelectedCandidateId { get; set; }

    public string ResolutionState { get; set; } = string.Empty;
}
