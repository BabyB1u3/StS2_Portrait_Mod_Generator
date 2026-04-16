using System.Text.Json;
using PortraitModGenerator.Core.Abstractions;

namespace PortraitModGenerator.Core.Services;

public sealed class MergeMappingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly ConflictResolutionService _conflictResolutionService = new();

    public MergedReviewSession Merge(MergeMappingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string sessionRoot = Path.GetFullPath(request.SessionRoot);
        string outputJsonPath = Path.GetFullPath(request.OutputJsonPath);
        string officialCardIndexPath = Path.GetFullPath(request.OfficialCardIndexPath);

        Directory.CreateDirectory(sessionRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(outputJsonPath)!);

        List<ImportedPackage> packages = request.Packages
            .Select(package => new ImportedPackage
            {
                PackageId = package.PackageId,
                DisplayName = package.DisplayName,
                SourcePckPath = Path.GetFullPath(package.SourcePckPath),
                RecoverRoot = Path.GetFullPath(package.RecoverRoot),
                ScanResultPath = Path.GetFullPath(package.ScanResultPath),
                MappingAnalysisPath = Path.GetFullPath(package.MappingAnalysisPath),
                ImportedAt = package.ImportedAt,
                ImportOrder = package.ImportOrder
            })
            .OrderBy(package => package.ImportOrder)
            .ThenBy(package => package.ImportedAt)
            .ToList();

        Dictionary<string, ImportedPackage> packageById = packages
            .ToDictionary(package => package.PackageId, StringComparer.OrdinalIgnoreCase);

        List<MergedMappingCandidate> mergedCandidates = [];

        foreach (ImportedPackage package in packages)
        {
            MappingAnalysisResult analysis = LoadAnalysis(package.MappingAnalysisPath);
            int index = 0;
            foreach (MappingCandidate candidate in analysis.Candidates)
            {
                mergedCandidates.Add(new MergedMappingCandidate
                {
                    CandidateId = $"{package.PackageId}:{index:D4}",
                    SourcePackageId = package.PackageId,
                    SourcePackageName = package.DisplayName,
                    SourceAbsolutePath = candidate.SourceAbsolutePath,
                    SourceRelativePath = candidate.RelativePath,
                    FileName = candidate.FileName,
                    Selected = candidate.Selected,
                    Ignored = candidate.Ignored,
                    IgnoredReason = candidate.IgnoredReason,
                    MatchedCardId = candidate.MatchedCardId,
                    CanonicalName = candidate.CanonicalName,
                    Group = candidate.Group,
                    Confidence = candidate.Confidence,
                    MatchReason = candidate.MatchReason
                });
                index++;
            }
        }

        ApplyDefaultSelections(mergedCandidates, packageById);

        MergedReviewSession session = new()
        {
            SessionId = request.SessionId,
            SessionRoot = sessionRoot,
            OfficialCardIndexPath = officialCardIndexPath,
            OutputJsonPath = outputJsonPath,
            Packages = packages,
            Candidates = mergedCandidates,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _conflictResolutionService.Refresh(session);
        File.WriteAllText(outputJsonPath, JsonSerializer.Serialize(session, JsonOptions));
        return session;
    }

    private static MappingAnalysisResult LoadAnalysis(string mappingAnalysisPath)
    {
        if (!File.Exists(mappingAnalysisPath))
        {
            throw new FileNotFoundException("Mapping analysis result file was not found.", mappingAnalysisPath);
        }

        string json = File.ReadAllText(mappingAnalysisPath);
        MappingAnalysisResult? analysis = JsonSerializer.Deserialize<MappingAnalysisResult>(json, JsonOptions);
        if (analysis is null)
        {
            throw new InvalidOperationException($"Failed to deserialize mapping analysis result '{mappingAnalysisPath}'.");
        }

        return analysis;
    }

    private static void ApplyDefaultSelections(
        IReadOnlyList<MergedMappingCandidate> candidates,
        IReadOnlyDictionary<string, ImportedPackage> packageById)
    {
        foreach (MergedMappingCandidate candidate in candidates)
        {
            candidate.Selected = !candidate.Ignored &&
                                 candidate.Selected &&
                                 !string.IsNullOrWhiteSpace(candidate.MatchedCardId);
            candidate.IsAutoSelected = false;
        }

        foreach (IGrouping<string, MergedMappingCandidate> group in candidates
                     .Where(candidate => !candidate.Ignored && !string.IsNullOrWhiteSpace(candidate.MatchedCardId))
                     .GroupBy(candidate => candidate.MatchedCardId!, StringComparer.OrdinalIgnoreCase))
        {
            List<MergedMappingCandidate> groupCandidates = group.ToList();
            if (groupCandidates.Count == 1)
            {
                groupCandidates[0].Selected = true;
                groupCandidates[0].IsAutoSelected = true;
                continue;
            }

            MergedMappingCandidate winner = ConflictResolutionService.ChooseDefaultCandidate(groupCandidates, packageById);
            foreach (MergedMappingCandidate candidate in groupCandidates)
            {
                candidate.Selected = string.Equals(candidate.CandidateId, winner.CandidateId, StringComparison.OrdinalIgnoreCase);
                candidate.IsAutoSelected = candidate.Selected;
            }
        }
    }
}
