using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using System.Diagnostics.CodeAnalysis;

namespace RegentFemCards.RegentFemCardsCode;

[HarmonyPatch(typeof(CardModel), "PortraitPath", MethodType.Getter)]
internal static class CardPortraitReplacementPatch
{
    [SuppressMessage("Style", "IDE0051", Justification = "Harmony discovers patch methods via reflection.")]
    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires the __instance parameter name.")]
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref string __result)
    {
        if (PortraitReplacementRegistry.TryGetPath(__instance, out string? replacementPath) &&
            !string.IsNullOrWhiteSpace(replacementPath))
        {
            __result = replacementPath;
        }
    }
}
