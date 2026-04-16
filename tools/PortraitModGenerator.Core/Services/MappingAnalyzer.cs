using System.Text.Json;
using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Models;

namespace PortraitModGenerator.Core.Services;

public sealed class MappingAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MappingAnalysisResult Analyze(MappingAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string scanResultPath = Path.GetFullPath(request.ScanResultPath);
        string officialCardIndexPath = Path.GetFullPath(request.OfficialCardIndexPath);
        string outputJsonPath = Path.GetFullPath(request.OutputJsonPath);

        if (!File.Exists(scanResultPath))
        {
            throw new FileNotFoundException("Asset scan result file was not found.", scanResultPath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)!);

        AssetScanResult scanResult = LoadScanResult(scanResultPath);
        OfficialCardIndex officialCardIndex = new OfficialCardIndexLoader().Load(officialCardIndexPath);
        NameCanonicalizer canonicalizer = new();

        Dictionary<string, List<OfficialCardEntry>> byCardId = officialCardIndex.Cards
            .GroupBy(card => card.CardId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<OfficialCardEntry>> byCanonicalName = officialCardIndex.Cards
            .GroupBy(card => card.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        HashSet<string> knownGroups = officialCardIndex.Cards
            .Select(card => card.Group)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<MappingCandidate> candidates = [];

        foreach (ScannedAsset asset in scanResult.Assets.OrderBy(asset => asset.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            NameCanonicalizer.CanonicalizedName canonicalizedName = canonicalizer.Canonicalize(asset.FileName);
            if (canonicalizedName.Ignored)
            {
                candidates.Add(new MappingCandidate
                {
                    SourceAbsolutePath = asset.AbsolutePath,
                    RelativePath = asset.RelativePath,
                    FileName = asset.FileName,
                    Selected = false,
                    Ignored = true,
                    IgnoredReason = canonicalizedName.IgnoredReason,
                    Confidence = 0
                });
                continue;
            }

            MappingCandidate candidate = CreateCandidate(asset, canonicalizedName.Keys, byCardId, byCanonicalName, knownGroups);
            candidates.Add(candidate);
        }

        MappingAnalysisResult result = new()
        {
            ScanResultPath = scanResultPath,
            OfficialCardIndexPath = officialCardIndexPath,
            OutputJsonPath = outputJsonPath,
            TotalAssets = scanResult.Assets.Count,
            MatchedAssets = candidates.Count(candidate => candidate.MatchedCardId is not null),
            IgnoredAssets = candidates.Count(candidate => candidate.Ignored),
            Candidates = candidates
        };

        File.WriteAllText(outputJsonPath, JsonSerializer.Serialize(result, JsonOptions));
        return result;
    }

    private static AssetScanResult LoadScanResult(string scanResultPath)
    {
        string json = File.ReadAllText(scanResultPath);
        AssetScanResult? result = JsonSerializer.Deserialize<AssetScanResult>(json, JsonOptions);
        if (result is null)
        {
            throw new InvalidOperationException($"Failed to deserialize asset scan result '{scanResultPath}'.");
        }

        return result;
    }

    private static MappingCandidate CreateCandidate(
        ScannedAsset asset,
        IReadOnlyList<string> keys,
        IReadOnlyDictionary<string, List<OfficialCardEntry>> byCardId,
        IReadOnlyDictionary<string, List<OfficialCardEntry>> byCanonicalName,
        IReadOnlySet<string> knownGroups)
    {
        string? groupHint = GetGroupHint(asset.RelativePath, knownGroups);

        foreach (string key in keys)
        {
            if (byCardId.TryGetValue(key, out List<OfficialCardEntry>? byCardIdMatches))
            {
                return ResolveMatch(asset, byCardIdMatches, groupHint, 1.0, $"Exact cardId match: {key}");
            }
        }

        foreach (string key in keys)
        {
            if (byCanonicalName.TryGetValue(key, out List<OfficialCardEntry>? byCanonicalNameMatches))
            {
                return ResolveMatch(asset, byCanonicalNameMatches, groupHint, 0.95, $"CanonicalName match: {key}");
            }
        }

        return new MappingCandidate
        {
            SourceAbsolutePath = asset.AbsolutePath,
            RelativePath = asset.RelativePath,
            FileName = asset.FileName,
            Selected = false,
            Ignored = false,
            Confidence = 0,
            MatchReason = "No deterministic match found."
        };
    }

    private static MappingCandidate ResolveMatch(
        ScannedAsset asset,
        IReadOnlyList<OfficialCardEntry> matches,
        string? groupHint,
        double confidence,
        string reason)
    {
        if (matches.Count == 1)
        {
            return BuildMatchedCandidate(asset, matches[0], confidence, reason);
        }

        if (!string.IsNullOrWhiteSpace(groupHint))
        {
            OfficialCardEntry[] groupMatches = matches
                .Where(match => string.Equals(match.Group, groupHint, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (groupMatches.Length == 1)
            {
                return BuildMatchedCandidate(
                    asset,
                    groupMatches[0],
                    confidence,
                    $"{reason}; resolved by path group hint: {groupHint}");
            }
        }

        return new MappingCandidate
        {
            SourceAbsolutePath = asset.AbsolutePath,
            RelativePath = asset.RelativePath,
            FileName = asset.FileName,
            Selected = false,
            Ignored = false,
            Confidence = 0,
            MatchReason = $"Ambiguous official match: {string.Join(", ", matches.Select(match => match.Group))}"
        };
    }

    private static MappingCandidate BuildMatchedCandidate(
        ScannedAsset asset,
        OfficialCardEntry match,
        double confidence,
        string reason)
    {
        return new MappingCandidate
        {
            SourceAbsolutePath = asset.AbsolutePath,
            RelativePath = asset.RelativePath,
            FileName = asset.FileName,
            Selected = true,
            Ignored = false,
            MatchedCardId = match.CardId,
            CanonicalName = match.CanonicalName,
            Group = match.Group,
            Confidence = confidence,
            MatchReason = reason
        };
    }

    private static string? GetGroupHint(string relativePath, IReadOnlySet<string> knownGroups)
    {
        string[] segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (string segment in segments)
        {
            if (knownGroups.Contains(segment))
            {
                return segment;
            }
        }

        return null;
    }
}
