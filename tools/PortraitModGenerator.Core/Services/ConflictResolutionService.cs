using PortraitModGenerator.Core.Abstractions;

namespace PortraitModGenerator.Core.Services;

public sealed class ConflictResolutionService
{
    public void Refresh(MergedReviewSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Dictionary<string, ImportedPackage> packageById = session.Packages
            .ToDictionary(package => package.PackageId, StringComparer.OrdinalIgnoreCase);

        foreach (MergedMappingCandidate candidate in session.Candidates)
        {
            candidate.IsConflict = false;
            candidate.IsAutoSelected = false;
            if (candidate.Ignored)
            {
                candidate.Selected = false;
            }
        }

        List<CardConflictGroup> conflictGroups = [];
        List<ResolvedMapping> resolvedMappings = [];

        foreach (IGrouping<string, MergedMappingCandidate> group in session.Candidates
                     .Where(candidate => !string.IsNullOrWhiteSpace(candidate.MatchedCardId))
                     .GroupBy(candidate => candidate.MatchedCardId!, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            List<MergedMappingCandidate> candidates = group.ToList();
            List<MergedMappingCandidate> activeCandidates = candidates
                .Where(candidate => !candidate.Ignored)
                .ToList();

            foreach (MergedMappingCandidate candidate in candidates)
            {
                candidate.IsConflict = candidates.Count > 1;
            }

            List<MergedMappingCandidate> selectedCandidates = activeCandidates
                .Where(candidate => candidate.Selected)
                .ToList();

            if (selectedCandidates.Count > 1)
            {
                MergedMappingCandidate winner = ChooseDefaultCandidate(selectedCandidates, packageById);
                foreach (MergedMappingCandidate candidate in activeCandidates)
                {
                    candidate.Selected = string.Equals(candidate.CandidateId, winner.CandidateId, StringComparison.OrdinalIgnoreCase);
                }

                selectedCandidates = [winner];
            }

            if (candidates.Count > 1)
            {
                conflictGroups.Add(new CardConflictGroup
                {
                    CardId = group.Key,
                    CanonicalName = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate.CanonicalName))?.CanonicalName ?? group.Key,
                    Group = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate.Group))?.Group ?? "unassigned",
                    CandidateIds = candidates
                        .Select(candidate => candidate.CandidateId)
                        .ToList(),
                    SelectedCandidateId = selectedCandidates.Count == 1 ? selectedCandidates[0].CandidateId : null,
                    ResolutionState = DetermineResolutionState(candidates, selectedCandidates.Count == 1)
                });
            }

            if (selectedCandidates.Count == 1)
            {
                MergedMappingCandidate selected = selectedCandidates[0];
                resolvedMappings.Add(new ResolvedMapping
                {
                    CandidateId = selected.CandidateId,
                    CardId = selected.MatchedCardId ?? string.Empty,
                    CanonicalName = selected.CanonicalName ?? selected.MatchedCardId ?? string.Empty,
                    Group = selected.Group ?? "unassigned",
                    SourcePackageId = selected.SourcePackageId,
                    SourcePackageName = selected.SourcePackageName,
                    SourceAbsolutePath = selected.SourceAbsolutePath,
                    SourceRelativePath = selected.SourceRelativePath,
                    FileName = selected.FileName,
                    Confidence = selected.Confidence
                });
            }
        }

        session.ConflictGroups = conflictGroups;
        session.ResolvedMappings = resolvedMappings;
        session.TotalAssets = session.Candidates.Count;
        session.MatchedAssets = session.Candidates.Count(candidate => !string.IsNullOrWhiteSpace(candidate.MatchedCardId));
        session.UnmatchedAssets = session.Candidates.Count(candidate => !candidate.Ignored && string.IsNullOrWhiteSpace(candidate.MatchedCardId));
        session.IgnoredAssets = session.Candidates.Count(candidate => candidate.Ignored);
        session.PendingAssets = session.Candidates.Count(candidate => !candidate.Ignored && (!candidate.Selected || string.IsNullOrWhiteSpace(candidate.MatchedCardId)));
        session.ResolvedAssets = session.ResolvedMappings.Count;
        session.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string DetermineResolutionState(IReadOnlyList<MergedMappingCandidate> candidates, bool hasSelectedCandidate)
    {
        if (candidates.All(candidate => candidate.Ignored))
        {
            return "Ignored";
        }

        if (hasSelectedCandidate)
        {
            return "Resolved";
        }

        return "Pending";
    }

    internal static MergedMappingCandidate ChooseDefaultCandidate(
        IReadOnlyList<MergedMappingCandidate> candidates,
        IReadOnlyDictionary<string, ImportedPackage> packageById)
    {
        return candidates
            .OrderByDescending(candidate => packageById.TryGetValue(candidate.SourcePackageId, out ImportedPackage? package) ? package.ImportOrder : 0)
            .ThenByDescending(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.SourceRelativePath, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
