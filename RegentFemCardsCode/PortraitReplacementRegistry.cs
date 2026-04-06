using Godot;
using MegaCrit.Sts2.Core.Models;

namespace RegentFemCards.RegentFemCardsCode;

internal static class PortraitReplacementRegistry
{
    public static void EnsureLoaded()
    {
        CardReplacementConfig.EnsureLoaded();
    }

    public static bool TryGetPath(CardModel model, out string? path)
    {
        EnsureLoaded();

        path = null;
        if (!CardReplacementConfig.TryGetEntry(model.GetType().Name, out CardReplacementConfig.ReplacementEntry? entry) ||
            entry is null ||
            string.IsNullOrWhiteSpace(entry.Portrait))
        {
            return false;
        }

        if (!ResourceLoader.Exists(entry.Portrait))
        {
            GD.PushWarning($"[RegentFemCards] Portrait file not found for {model.GetType().Name}: {entry.Portrait}");
            return false;
        }

        path = entry.Portrait;
        return true;
    }
}
