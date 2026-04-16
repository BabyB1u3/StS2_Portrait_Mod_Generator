using System.Text.Json;
using System.Text.Json.Serialization;
using PortraitModGenerator.Core.Abstractions;

namespace PortraitModGenerator.Core.Services;

public sealed class MappingMaterializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

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

        MappingAnalysisResult analysis = LoadAnalysis(mappingAnalysisPath);
        string configPath = Path.Combine(modProjectRoot, request.ModId, "config", "card_replacements.json");
        string portraitRoot = Path.Combine(modProjectRoot, request.ModId, "CardPortraits");

        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        Directory.CreateDirectory(portraitRoot);

        List<ReplacementEntry> entries = [];
        int portraitsCopied = 0;

        foreach (MappingCandidate candidate in analysis.Candidates
                     .Where(candidate => candidate.Selected && !candidate.Ignored && candidate.CanonicalName is not null)
                     .OrderBy(candidate => candidate.CanonicalName, StringComparer.OrdinalIgnoreCase))
        {
            string sourcePath = candidate.SourceAbsolutePath;
            if (!File.Exists(sourcePath))
            {
                continue;
            }

            string extension = Path.GetExtension(sourcePath);
            string groupFolder = ToDisplayGroup(candidate.Group ?? "unassigned");
            string destinationDirectory = Path.Combine(portraitRoot, groupFolder);
            string destinationFileName = $"{candidate.CanonicalName}{extension}";
            string destinationPath = Path.Combine(destinationDirectory, destinationFileName);

            Directory.CreateDirectory(destinationDirectory);
            File.Copy(sourcePath, destinationPath, overwrite: true);
            portraitsCopied++;

            entries.Add(new ReplacementEntry
            {
                CardId = candidate.CanonicalName!,
                Portrait = $"res://{request.ModId}/CardPortraits/{groupFolder}/{destinationFileName}"
            });
        }

        ReplacementDocument document = new()
        {
            Entries = entries
        };

        File.WriteAllText(configPath, JsonSerializer.Serialize(document, JsonOptions));

        return new MaterializeMappingsResult
        {
            MappingAnalysisPath = mappingAnalysisPath,
            ModProjectRoot = modProjectRoot,
            ConfigPath = configPath,
            PortraitRoot = portraitRoot,
            EntriesWritten = entries.Count,
            PortraitsCopied = portraitsCopied
        };
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

    private sealed class ReplacementEntry
    {
        [JsonPropertyName("cardId")]
        public string CardId { get; set; } = string.Empty;

        [JsonPropertyName("portrait")]
        public string Portrait { get; set; } = string.Empty;
    }
}
