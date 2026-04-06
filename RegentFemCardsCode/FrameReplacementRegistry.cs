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

    public static void EnsureLoaded()
    {
        CardReplacementConfig.EnsureLoaded();
    }

    public static bool TryGetEntry(string? cardId, out FrameReplacementEntry entry)
    {
        EnsureLoaded();

        if (string.IsNullOrWhiteSpace(cardId))
        {
            entry = new FrameReplacementEntry();
            return false;
        }

        if (!CardReplacementConfig.TryGetEntry(cardId, out CardReplacementConfig.ReplacementEntry? sourceEntry) ||
            sourceEntry is null)
        {
            entry = new FrameReplacementEntry();
            return false;
        }

        entry = new FrameReplacementEntry
        {
            CardId = sourceEntry.CardId,
            UiMode = sourceEntry.UiMode,
            Frame = sourceEntry.Frame,
            FrameMaterial = sourceEntry.FrameMaterial,
            BannerTexture = sourceEntry.BannerTexture,
            BannerMaterial = sourceEntry.BannerMaterial,
            PortraitBorder = sourceEntry.PortraitBorder,
            PortraitBorderMaterial = sourceEntry.PortraitBorderMaterial,
            AncientTextBackground = sourceEntry.AncientTextBackground,
            TextBackgroundMaterial = sourceEntry.TextBackgroundMaterial,
            EnergyIcon = sourceEntry.EnergyIcon,
            Highlight = sourceEntry.Highlight,
            HighlightMaterial = sourceEntry.HighlightMaterial
        };

        return !(string.IsNullOrWhiteSpace(entry.Frame) &&
                 string.IsNullOrWhiteSpace(entry.FrameMaterial) &&
                 string.IsNullOrWhiteSpace(entry.BannerTexture) &&
                 string.IsNullOrWhiteSpace(entry.BannerMaterial) &&
                 string.IsNullOrWhiteSpace(entry.PortraitBorder) &&
                 string.IsNullOrWhiteSpace(entry.PortraitBorderMaterial) &&
                 string.IsNullOrWhiteSpace(entry.AncientTextBackground) &&
                 string.IsNullOrWhiteSpace(entry.TextBackgroundMaterial) &&
                 string.IsNullOrWhiteSpace(entry.EnergyIcon) &&
                 string.IsNullOrWhiteSpace(entry.Highlight) &&
                 string.IsNullOrWhiteSpace(entry.HighlightMaterial) &&
                 string.IsNullOrWhiteSpace(entry.UiMode));
    }

    public static bool ShouldSpoofAncientUi(string? cardId)
    {
        return TryGetEntry(cardId, out FrameReplacementEntry entry) &&
               string.Equals(entry.UiMode, "Ancient", StringComparison.OrdinalIgnoreCase);
    }
}
