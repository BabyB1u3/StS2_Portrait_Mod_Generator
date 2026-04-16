namespace PortraitModGenerator.Core.Abstractions;

public sealed class AssetScanResult
{
    public required string InputDirectory { get; init; }

    public required string OutputJsonPath { get; init; }

    public required int TotalFilesScanned { get; init; }

    public required int ImageFilesFound { get; init; }

    public required IReadOnlyList<ScannedAsset> Assets { get; init; }
}
