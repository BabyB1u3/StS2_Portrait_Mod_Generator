using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RegentFemCards.RegentFemCardsCode;

[HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
internal static class CardUiModeSpoofPatch
{
    private static readonly string[] AllowedTypePrefixes =
    [
        "MegaCrit.Sts2.Core.Nodes.Cards.",
        "MegaCrit.Sts2.Core.Nodes.HoverTips."
    ];

    private static readonly string[] AllowedExactTypes =
    [
        "MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantCard",
        "MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NDeckHistoryEntry"
    ];

    [SuppressMessage("Style", "IDE0051", Justification = "Harmony discovers patch methods via reflection.")]
    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires the __instance parameter name.")]
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref CardRarity __result)
    {
        if (__result == CardRarity.Ancient)
        {
            return;
        }

        string cardId = __instance.GetType().FullName ?? __instance.GetType().Name;
        if (FrameReplacementRegistry.ShouldSpoofAncientUi(cardId) && ShouldSpoofForUi())
        {
            __result = CardRarity.Ancient;
        }
    }

    internal static bool ShouldSpoofForUi()
    {
        try
        {
            StackFrame[] frames = new StackTrace(fNeedFileInfo: false).GetFrames() ?? Array.Empty<StackFrame>();
            foreach (StackFrame frame in frames)
            {
                Type? type = frame.GetMethod()?.DeclaringType;
                string? fullName = type?.FullName;
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    continue;
                }

                if (AllowedExactTypes.Any(typeName => string.Equals(fullName, typeName, StringComparison.Ordinal)))
                {
                    return true;
                }

                if (AllowedTypePrefixes.Any(prefix => fullName.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[RegentFemCards] CardUiModeSpoofPatch stack inspect failed: {ex.Message}");
        }

        return false;
    }
}

[HarmonyPatch(typeof(NTinyCard), "GetBannerColor")]
internal static class TinyCardBannerColorPatch
{
    [SuppressMessage("Style", "IDE0051", Justification = "Harmony discovers patch methods via reflection.")]
    [HarmonyPostfix]
    private static void Postfix(ref Color __result, CardRarity rarity)
    {
        if (rarity != CardRarity.Ancient && CardUiModeSpoofPatch.ShouldSpoofForUi())
        {
            __result = new Color(0.82f, 0.68f, 0.34f);
        }
    }
}
