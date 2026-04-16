namespace PortraitModGenerator.Core.Abstractions;

public sealed class AssetScanRequest
{
    public required string InputDirectory { get; init; }

    public string? OutputJsonPath { get; init; }
}
