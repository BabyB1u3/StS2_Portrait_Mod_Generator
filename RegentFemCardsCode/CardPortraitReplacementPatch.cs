using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System.Reflection;

namespace RegentFemCards.RegentFemCardsCode;

[HarmonyPatch]
internal static class CardPortraitReplacementPatch
{
    private static readonly BindingFlags InstanceBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo? PortraitField =
        typeof(NCard).GetField("_portrait", InstanceBindings);

    private static readonly FieldInfo? AncientPortraitField =
        typeof(NCard).GetField("_ancientPortrait", InstanceBindings);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static void UpdateVisualsPostfix(NCard __instance, PileType pileType, CardPreviewMode previewMode)
    {
        TryApplyReplacement(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard._EnterTree))]
    private static void EnterTreePostfix(NCard __instance)
    {
        TryApplyReplacement(__instance);
    }

    private static void TryApplyReplacement(NCard cardNode)
    {
        if (cardNode.Model is not CardModel model)
        {
            return;
        }

        if (!PortraitReplacementRegistry.TryGetTexture(model, out Texture2D? texture) || texture is null)
        {
            return;
        }

        ApplyTexture(PortraitField?.GetValue(cardNode) as TextureRect, texture);
        ApplyTexture(AncientPortraitField?.GetValue(cardNode) as TextureRect, texture);
    }

    private static void ApplyTexture(TextureRect? textureRect, Texture2D texture)
    {
        if (textureRect is null)
        {
            return;
        }

        textureRect.Texture = texture;
        textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        textureRect.Visible = true;
    }
}
