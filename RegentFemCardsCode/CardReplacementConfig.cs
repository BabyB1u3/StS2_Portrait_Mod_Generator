using Godot;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RegentFemCards.RegentFemCardsCode;

internal static class CardReplacementConfig
{
    private const string ConfigPath = "res://RegentFemCards/config/card_replacements.json";

    private sealed class ReplacementDocument
    {
        [JsonPropertyName("entries")]
        public List<ReplacementEntry> Entries { get; set; } = [];
    }

    internal sealed class ReplacementEntry
    {
        [JsonPropertyName("cardId")]
        public string CardId { get; set; } = string.Empty;

        [JsonPropertyName("portrait")]
        public string Portrait { get; set; } = string.Empty;

        [JsonPropertyName("uiMode")]
        public string UiMode { get; set; } = string.Empty;

        [JsonPropertyName("frame")]
        public string Frame { get; set; } = string.Empty;

        [JsonPropertyName("frameMaterial")]
        public string FrameMaterial { get; set; } = string.Empty;

        [JsonPropertyName("bannerTexture")]
        public string BannerTexture { get; set; } = string.Empty;

        [JsonPropertyName("bannerMaterial")]
        public string BannerMaterial { get; set; } = string.Empty;

        [JsonPropertyName("portraitBorder")]
        public string PortraitBorder { get; set; } = string.Empty;

        [JsonPropertyName("portraitBorderMaterial")]
        public string PortraitBorderMaterial { get; set; } = string.Empty;

        [JsonPropertyName("ancientTextBg")]
        public string AncientTextBackground { get; set; } = string.Empty;

        [JsonPropertyName("textBackgroundMaterial")]
        public string TextBackgroundMaterial { get; set; } = string.Empty;

        [JsonPropertyName("energyIcon")]
        public string EnergyIcon { get; set; } = string.Empty;

        [JsonPropertyName("highlight")]
        public string Highlight { get; set; } = string.Empty;

        [JsonPropertyName("highlightMaterial")]
        public string HighlightMaterial { get; set; } = string.Empty;
    }

    private static readonly Dictionary<string, ReplacementEntry> Entries = new(StringComparer.OrdinalIgnoreCase);
    private static bool _loaded;

    public static int Count
    {
        get
        {
            EnsureLoaded();
            return Entries.Count;
        }
    }

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        Entries.Clear();

        try
        {
            string json = Godot.FileAccess.GetFileAsString(ConfigPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                GD.PushWarning($"[RegentFemCards] Replacement config missing or empty: {ConfigPath}");
                return;
            }

            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            ReplacementDocument? document = JsonSerializer.Deserialize<ReplacementDocument>(json, options);
            if (document?.Entries is null)
            {
                return;
            }

            foreach (ReplacementEntry entry in document.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.CardId))
                {
                    continue;
                }

                Entries[Normalize(entry.CardId)] = entry;
            }

            GD.Print($"[RegentFemCards] Loaded {Entries.Count} replacement config entr{(Entries.Count == 1 ? "y" : "ies")}.");
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[RegentFemCards] Failed to load replacement config: {ex.Message}");
            Entries.Clear();
        }
    }

    public static bool TryGetEntry(string? cardId, out ReplacementEntry? entry)
    {
        EnsureLoaded();

        entry = null;
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return false;
        }

        string normalizedCardId = Normalize(cardId);
        if (Entries.TryGetValue(normalizedCardId, out ReplacementEntry? exactMatch))
        {
            entry = exactMatch;
            return true;
        }

        string shortName = ExtractShortName(normalizedCardId);
        if (Entries.TryGetValue(shortName, out ReplacementEntry? shortMatch))
        {
            entry = shortMatch;
            return true;
        }

        return false;
    }

    private static string Normalize(string value)
    {
        return value.Trim();
    }

    private static string ExtractShortName(string value)
    {
        int index = value.LastIndexOf('.');
        return index >= 0 ? value[(index + 1)..] : value;
    }
}
