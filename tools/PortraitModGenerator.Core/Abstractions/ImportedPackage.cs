using System.Text.Json.Serialization;

namespace PortraitModGenerator.Core.Abstractions;

public sealed class ImportedPackage
{
    public string PackageId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SourcePckPath { get; set; } = string.Empty;

    public string RecoverRoot { get; set; } = string.Empty;

    public string ScanResultPath { get; set; } = string.Empty;

    public string MappingAnalysisPath { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; }

    public int ImportOrder { get; set; }

    [JsonIgnore]
    public string ListDisplayText => $"{ImportOrder:00}. {DisplayName}";
}
