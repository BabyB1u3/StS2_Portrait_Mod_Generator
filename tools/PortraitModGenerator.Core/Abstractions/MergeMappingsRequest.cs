namespace PortraitModGenerator.Core.Abstractions;

public sealed class MergeMappingsRequest
{
    public string SessionId { get; init; } = string.Empty;

    public string SessionRoot { get; init; } = string.Empty;

    public string OfficialCardIndexPath { get; init; } = string.Empty;

    public string OutputJsonPath { get; init; } = string.Empty;

    public IReadOnlyList<ImportedPackage> Packages { get; init; } = [];
}
