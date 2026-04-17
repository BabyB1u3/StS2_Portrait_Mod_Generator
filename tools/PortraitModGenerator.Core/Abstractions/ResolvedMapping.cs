namespace PortraitModGenerator.Core.Abstractions;

public sealed class ResolvedMapping
{
    public string CandidateId { get; set; } = string.Empty;

    public string CardId { get; set; } = string.Empty;

    public string CanonicalName { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public string SourcePackageId { get; set; } = string.Empty;

    public string SourcePackageName { get; set; } = string.Empty;

    public string SourceAbsolutePath { get; set; } = string.Empty;

    public string SourceRelativePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public Dictionary<string, string>? AdvancedFields { get; set; }
}
