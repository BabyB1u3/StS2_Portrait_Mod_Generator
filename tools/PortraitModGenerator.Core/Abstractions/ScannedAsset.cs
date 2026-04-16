namespace PortraitModGenerator.Core.Abstractions;

public sealed class ScannedAsset
{
    public required string AbsolutePath { get; init; }

    public required string RelativePath { get; init; }

    public required string FileName { get; init; }

    public required string Extension { get; init; }

    public required long FileSize { get; init; }
}
