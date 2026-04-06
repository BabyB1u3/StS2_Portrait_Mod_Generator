using System;
using System.Collections.Generic;

namespace RegentFemCards.RegentFemCardsCode;

internal static class FrameReplacementRegistry
{
    internal sealed class FrameReplacementEntry
    {
        public string CardId { get; init; } = string.Empty;
        public string UiMode { get; init; } = string.Empty;
        public string Frame { get; init; } = string.Empty;
        public string FrameMaterial { get; init; } = string.Empty;
        public string BannerTexture { get; init; } = string.Empty;
        public string BannerMaterial { get; init; } = string.Empty;
        public string PortraitBorder { get; init; } = string.Empty;
        public string PortraitBorderMaterial { get; init; } = string.Empty;
        public string AncientTextBackground { get; init; } = string.Empty;
        public string TextBackgroundMaterial { get; init; } = string.Empty;
        public string EnergyIcon { get; init; } = string.Empty;
        public string Highlight { get; init; } = string.Empty;
        public string HighlightMaterial { get; init; } = string.Empty;
    }

    private static readonly Dictionary<string, FrameReplacementEntry> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SovereignBlade"] = new FrameReplacementEntry
        {
            CardId = "SovereignBlade",
            UiMode = "Ancient",
            Frame = "res://images/atlases/compressed.sprites/card_template/ancient_card_border.tres",
            BannerTexture = "res://images/atlases/ui_atlas.sprites/card/ancient_banner.tres",
            PortraitBorder = "res://images/atlases/ui_atlas.sprites/card/card_portrait_border_attack_s.tres",
            AncientTextBackground = "res://images/atlases/compressed.sprites/card_template/ancient_card_text_bg_attack.tres",
            Highlight = "res://images/atlases/compressed.sprites/card_template/card_highlight_ancient.tres",
        },
    };

    public static void EnsureLoaded()
    {
        // Hardcoded config for now. Extend Entries with more cards when needed.
    }

    public static bool TryGetEntry(string? cardId, out FrameReplacementEntry entry)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            entry = new FrameReplacementEntry();
            return false;
        }

        string normalizedCardId = Normalize(cardId);
        if (Entries.TryGetValue(normalizedCardId, out FrameReplacementEntry? existing))
        {
            entry = existing;
            return true;
        }

        string shortName = ExtractShortName(normalizedCardId);
        if (Entries.TryGetValue(shortName, out existing))
        {
            entry = existing;
            return true;
        }

        entry = new FrameReplacementEntry();
        return false;
    }

    public static bool ShouldSpoofAncientUi(string? cardId)
    {
        return TryGetEntry(cardId, out FrameReplacementEntry entry) &&
               string.Equals(entry.UiMode, "Ancient", StringComparison.OrdinalIgnoreCase);
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
