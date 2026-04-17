using System.Text.Json;
using System.Text.Json.Serialization;
using PortraitModGenerator.Core.Abstractions;

namespace PortraitModGenerator.Core.Services;

public sealed class MappingMaterializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static IReadOnlyList<string> AdvancedFieldKeys { get; } =
    [
        "uiMode",
        "frame",
        "frameMaterial",
        "bannerTexture",
        "bannerMaterial",
        "portraitBorder",
        "portraitBorderMaterial",
        "ancientTextBg",
        "textBackgroundMaterial",
        "energyIcon",
        "highlight",
        "highlightMaterial"
    ];

    public MaterializeMappingsResult Materialize(MaterializeMappingsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string mappingAnalysisPath = Path.GetFullPath(request.MappingAnalysisPath);
        string modProjectRoot = Path.GetFullPath(request.ModProjectRoot);

        if (!File.Exists(mappingAnalysisPath))
        {
            throw new FileNotFoundException("Mapping analysis result file was not found.", mappingAnalysisPath);
        }

        if (!Directory.Exists(modProjectRoot))
        {
            throw new DirectoryNotFoundException($"Mod project root not found: {modProjectRoot}");
        }

        string configPath = Path.Combine(modProjectRoot, request.ModId, "config", "card_replacements.json");
        string portraitRoot = Path.Combine(modProjectRoot, request.ModId, "CardPortraits");

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        Directory.CreateDirectory(portraitRoot);

        IReadOnlyList<ReplacementEntry> entriesToWrite;
        int portraitsCopied;
        if (TryLoadMergedSession(mappingAnalysisPath, out MergedReviewSession? mergedSession))
        {
            (entriesToWrite, portraitsCopied) = MaterializeResolvedMappings(mergedSession!, portraitRoot, request.ModId);
        }
        else
        {
            MappingAnalysisResult analysis = LoadAnalysis(mappingAnalysisPath);
            (entriesToWrite, portraitsCopied) = MaterializeCandidates(analysis.Candidates, portraitRoot, request.ModId);
        }

        ReplacementDocument document = new()
        {
            Entries = entriesToWrite.ToList()
        };

        File.WriteAllText(configPath, JsonSerializer.Serialize(document, JsonOptions));

        return new MaterializeMappingsResult
        {
            MappingAnalysisPath = mappingAnalysisPath,
            ModProjectRoot = modProjectRoot,
            ConfigPath = configPath,
            PortraitRoot = portraitRoot,
            EntriesWritten = entriesToWrite.Count,
            PortraitsCopied = portraitsCopied
        };
    }

    private static (IReadOnlyList<ReplacementEntry> Entries, int PortraitsCopied) MaterializeResolvedMappings(
        MergedReviewSession session,
        string portraitRoot,
        string modId)
    {
        List<ReplacementEntry> entries = [];
        int portraitsCopied = 0;

        foreach (ResolvedMapping mapping in session.ResolvedMappings
                     .OrderBy(mapping => mapping.CanonicalName, StringComparer.OrdinalIgnoreCase))
        {
            if (!TryCopyPortrait(mapping.SourceAbsolutePath, portraitRoot, modId, mapping.Group, mapping.CanonicalName, out string? portraitPath))
            {
                continue;
            }

            portraitsCopied++;
            ReplacementEntry entry = new()
            {
                CardId = mapping.CanonicalName,
                Portrait = portraitPath!
            };
            ApplyAdvancedFields(entry, mapping.AdvancedFields);
            entries.Add(entry);
        }

        return (entries, portraitsCopied);
    }

    private static (IReadOnlyList<ReplacementEntry> Entries, int PortraitsCopied) MaterializeCandidates(
        IEnumerable<MappingCandidate> candidates,
        string portraitRoot,
        string modId)
    {
        List<ReplacementEntry> entries = [];
        int portraitsCopied = 0;

        foreach (MappingCandidate candidate in candidates
                     .Where(candidate => candidate.Selected && !candidate.Ignored && candidate.CanonicalName is not null)
                     .OrderBy(candidate => candidate.CanonicalName, StringComparer.OrdinalIgnoreCase))
        {
            if (!TryCopyPortrait(candidate.SourceAbsolutePath, portraitRoot, modId, candidate.Group, candidate.CanonicalName!, out string? portraitPath))
            {
                continue;
            }

            portraitsCopied++;
            entries.Add(new ReplacementEntry
            {
                CardId = candidate.CanonicalName!,
                Portrait = portraitPath!
            });
        }

        return (entries, portraitsCopied);
    }

    private static MappingAnalysisResult LoadAnalysis(string mappingAnalysisPath)
    {
        string json = File.ReadAllText(mappingAnalysisPath);
        MappingAnalysisResult? analysis = JsonSerializer.Deserialize<MappingAnalysisResult>(json, JsonOptions);
        if (analysis is null)
        {
            throw new InvalidOperationException($"Failed to deserialize mapping analysis result '{mappingAnalysisPath}'.");
        }

        return analysis;
    }

    private static bool TryLoadMergedSession(string mappingAnalysisPath, out MergedReviewSession? session)
    {
        string json = File.ReadAllText(mappingAnalysisPath);
        using JsonDocument document = JsonDocument.Parse(json);
        bool hasPackagesProperty = document.RootElement
            .EnumerateObject()
            .Any(property => string.Equals(property.Name, nameof(MergedReviewSession.Packages), StringComparison.OrdinalIgnoreCase));

        if (!hasPackagesProperty)
        {
            session = null;
            return false;
        }

        session = JsonSerializer.Deserialize<MergedReviewSession>(json, JsonOptions);
        if (session is null)
        {
            throw new InvalidOperationException($"Failed to deserialize merged review session '{mappingAnalysisPath}'.");
        }

        return true;
    }

    private static bool TryCopyPortrait(
        string sourcePath,
        string portraitRoot,
        string modId,
        string? group,
        string canonicalName,
        out string? portraitPath)
    {
        portraitPath = null;
        if (!File.Exists(sourcePath))
        {
            return false;
        }

        string extension = Path.GetExtension(sourcePath);
        string groupFolder = ToDisplayGroup(group ?? "unassigned");
        string destinationDirectory = Path.Combine(portraitRoot, groupFolder);
        string destinationFileName = $"{canonicalName}{extension}";
        string destinationPath = Path.Combine(destinationDirectory, destinationFileName);

        Directory.CreateDirectory(destinationDirectory);
        File.Copy(sourcePath, destinationPath, overwrite: true);
        portraitPath = $"res://{modId}/CardPortraits/{groupFolder}/{destinationFileName}";
        return true;
    }

    private static string ToDisplayGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            return "Unassigned";
        }

        return char.ToUpperInvariant(group[0]) + group[1..];
    }

    private sealed class ReplacementDocument
    {
        [JsonPropertyName("entries")]
        public List<ReplacementEntry> Entries { get; set; } = [];
    }

    private static void ApplyAdvancedFields(ReplacementEntry entry, IReadOnlyDictionary<string, string>? advancedFields)
    {
        if (advancedFields is null || advancedFields.Count == 0)
        {
            return;
        }

        foreach ((string key, string value) in advancedFields)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            switch (key)
            {
                case "uiMode": entry.UiMode = value; break;
                case "frame": entry.Frame = value; break;
                case "frameMaterial": entry.FrameMaterial = value; break;
                case "bannerTexture": entry.BannerTexture = value; break;
                case "bannerMaterial": entry.BannerMaterial = value; break;
                case "portraitBorder": entry.PortraitBorder = value; break;
                case "portraitBorderMaterial": entry.PortraitBorderMaterial = value; break;
                case "ancientTextBg": entry.AncientTextBackground = value; break;
                case "textBackgroundMaterial": entry.TextBackgroundMaterial = value; break;
                case "energyIcon": entry.EnergyIcon = value; break;
                case "highlight": entry.Highlight = value; break;
                case "highlightMaterial": entry.HighlightMaterial = value; break;
            }
        }
    }

    private sealed class ReplacementEntry
    {
        [JsonPropertyName("cardId")]
        public string CardId { get; set; } = string.Empty;

        [JsonPropertyName("portrait")]
        public string Portrait { get; set; } = string.Empty;

        [JsonPropertyName("uiMode")]
        public string? UiMode { get; set; }

        [JsonPropertyName("frame")]
        public string? Frame { get; set; }

        [JsonPropertyName("frameMaterial")]
        public string? FrameMaterial { get; set; }

        [JsonPropertyName("bannerTexture")]
        public string? BannerTexture { get; set; }

        [JsonPropertyName("bannerMaterial")]
        public string? BannerMaterial { get; set; }

        [JsonPropertyName("portraitBorder")]
        public string? PortraitBorder { get; set; }

        [JsonPropertyName("portraitBorderMaterial")]
        public string? PortraitBorderMaterial { get; set; }

        [JsonPropertyName("ancientTextBg")]
        public string? AncientTextBackground { get; set; }

        [JsonPropertyName("textBackgroundMaterial")]
        public string? TextBackgroundMaterial { get; set; }

        [JsonPropertyName("energyIcon")]
        public string? EnergyIcon { get; set; }

        [JsonPropertyName("highlight")]
        public string? Highlight { get; set; }

        [JsonPropertyName("highlightMaterial")]
        public string? HighlightMaterial { get; set; }
    }
}
