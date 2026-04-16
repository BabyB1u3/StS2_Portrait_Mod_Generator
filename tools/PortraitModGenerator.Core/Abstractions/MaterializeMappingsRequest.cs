namespace PortraitModGenerator.Core.Abstractions;

public sealed class MaterializeMappingsRequest
{
    public required string MappingAnalysisPath { get; init; }

    public required string ModProjectRoot { get; init; }

    public required string ModId { get; init; }
}
