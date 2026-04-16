namespace PortraitModGenerator.Core.Abstractions;

public sealed class MaterializeMappingsResult
{
    public required string MappingAnalysisPath { get; init; }

    public required string ModProjectRoot { get; init; }

    public required string ConfigPath { get; init; }

    public required string PortraitRoot { get; init; }

    public required int EntriesWritten { get; init; }

    public required int PortraitsCopied { get; init; }
}
