using System.Text.Json;
using PortraitModGenerator.Core.Abstractions;

namespace PortraitModGenerator.Core.Services;

public sealed class AssetScanner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private static readonly string[] IgnoredPathSegments =
    [
        $"{Path.DirectorySeparatorChar}.godot{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.gdre_user{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}"
    ];

    public AssetScanResult Scan(AssetScanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string inputDirectory = Path.GetFullPath(request.InputDirectory);
        if (!Directory.Exists(inputDirectory))
        {
            throw new DirectoryNotFoundException($"Input directory not found: {inputDirectory}");
        }

        string outputJsonPath = string.IsNullOrWhiteSpace(request.OutputJsonPath)
            ? Path.Combine(inputDirectory, "asset_scan_result.json")
            : Path.GetFullPath(request.OutputJsonPath);

        Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)!);

        List<ScannedAsset> assets = [];
        int totalFilesScanned = 0;

        foreach (string filePath in Directory.EnumerateFiles(inputDirectory, "*", SearchOption.AllDirectories))
        {
            totalFilesScanned++;

            if (ShouldIgnore(filePath))
            {
                continue;
            }

            string extension = Path.GetExtension(filePath);
            if (!SupportedImageExtensions.Contains(extension))
            {
                continue;
            }

            FileInfo info = new(filePath);
            assets.Add(new ScannedAsset
            {
                AbsolutePath = filePath,
                RelativePath = Path.GetRelativePath(inputDirectory, filePath),
                FileName = info.Name,
                Extension = extension,
                FileSize = info.Length
            });
        }

        AssetScanResult result = new()
        {
            InputDirectory = inputDirectory,
            OutputJsonPath = outputJsonPath,
            TotalFilesScanned = totalFilesScanned,
            ImageFilesFound = assets.Count,
            Assets = assets
        };

        File.WriteAllText(outputJsonPath, JsonSerializer.Serialize(result, JsonOptions));
        return result;
    }

    private static bool ShouldIgnore(string filePath)
    {
        string normalizedPath = Path.GetFullPath(filePath);
        if (normalizedPath.EndsWith(".import", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (string ignoredSegment in IgnoredPathSegments)
        {
            if (normalizedPath.Contains(ignoredSegment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
