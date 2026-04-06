using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RegentFemCards.RegentFemCardsCode;

[HarmonyPatch]
internal static class FramePatch
{
    private static readonly BindingFlags InstanceBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo? FrameField = typeof(NCard).GetField("_frame", InstanceBindings);
    private static readonly FieldInfo? BannerField = typeof(NCard).GetField("_banner", InstanceBindings);
    private static readonly FieldInfo? PortraitBorderField = typeof(NCard).GetField("_portraitBorder", InstanceBindings);
    private static readonly FieldInfo? AncientTextBgField = typeof(NCard).GetField("_ancientTextBg", InstanceBindings);
    private static readonly FieldInfo? HighlightField = typeof(NCard).GetField("_ancientHighlight", InstanceBindings);
    private static readonly FieldInfo? EnergyIconField = typeof(NCard).GetField("_energyIcon", InstanceBindings);
    private static readonly FieldInfo? AncientBannerField = typeof(NCard).GetField("_ancientBanner", InstanceBindings);

    [SuppressMessage("Style", "IDE0051", Justification = "Harmony discovers patch methods via reflection.")]
    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires the __instance parameter name.")]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static void UpdateVisualsPostfix(NCard __instance)
    {
        ApplyOverrides(__instance);
    }

    [SuppressMessage("Style", "IDE0051", Justification = "Harmony discovers patch methods via reflection.")]
    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires the __instance parameter name.")]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NCard), nameof(NCard._EnterTree))]
    private static void EnterTreePostfix(NCard __instance)
    {
        ApplyOverrides(__instance);
    }

    private static void ApplyOverrides(NCard cardNode)
    {
        CardModel? model = cardNode.Model;
        if (model is null)
        {
            return;
        }

        string cardId = model.GetType().FullName ?? model.GetType().Name;
        if (!FrameReplacementRegistry.TryGetEntry(cardId, out FrameReplacementRegistry.FrameReplacementEntry entry))
        {
            return;
        }

        ApplyLayer(FrameField, cardNode, entry.Frame, entry.FrameMaterial);
        ApplyLayer(BannerField, cardNode, entry.BannerTexture, entry.BannerMaterial);
        ApplyLayer(PortraitBorderField, cardNode, entry.PortraitBorder, entry.PortraitBorderMaterial);
        ApplyLayer(EnergyIconField, cardNode, entry.EnergyIcon, string.Empty);

        if (FrameReplacementRegistry.ShouldSpoofAncientUi(cardId))
        {
            ApplyAncientVisualOverrides(cardNode, entry);
            return;
        }

        ApplyLayer(AncientTextBgField, cardNode, entry.AncientTextBackground, entry.TextBackgroundMaterial);
        ApplyLayer(HighlightField, cardNode, entry.Highlight, entry.HighlightMaterial);
    }

    private static void ApplyAncientVisualOverrides(NCard cardNode, FrameReplacementRegistry.FrameReplacementEntry entry)
    {
        TextureRect? ancientBorder = FindTextureRectNode(cardNode, "AncientBorder");
        TextureRect? ancientBanner = FindFirstTextureRect(AncientBannerField?.GetValue(cardNode) as Control);
        TextureRect? ancientTextBg = FindTextureRectNode(cardNode, "AncientTextBg");
        TextureRect? ancientHighlight = FindTextureRectNode(cardNode, "AncientHighlight");

        ApplyLayer(ancientBorder, entry.Frame, entry.FrameMaterial);
        ApplyLayer(ancientBanner, entry.BannerTexture, entry.BannerMaterial);
        ApplyLayer(ancientTextBg, entry.AncientTextBackground, entry.TextBackgroundMaterial);
        ApplyLayer(ancientHighlight, entry.Highlight, entry.HighlightMaterial);
    }

    private static void ApplyLayer(FieldInfo? field, NCard cardNode, string texturePath, string materialPath)
    {
        if (field?.GetValue(cardNode) is not TextureRect textureRect)
        {
            return;
        }

        ApplyLayer(textureRect, texturePath, materialPath);
    }

    private static void ApplyLayer(TextureRect? textureRect, string texturePath, string materialPath)
    {
        if (textureRect is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(texturePath))
        {
            Texture2D? texture = GD.Load<Texture2D>(texturePath);
            if (texture is not null)
            {
                textureRect.Texture = texture;
                textureRect.Visible = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(materialPath))
        {
            Material? material = GD.Load<Material>(materialPath);
            if (material is not null)
            {
                textureRect.Material = material;
            }
        }
    }

    private static TextureRect? FindTextureRectNode(Node root, string nodeName)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is TextureRect rect &&
                string.Equals(rect.Name.ToString(), nodeName, StringComparison.OrdinalIgnoreCase))
            {
                return rect;
            }

            TextureRect? nested = FindTextureRectNode(child, nodeName);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static TextureRect? FindFirstTextureRect(Node? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is TextureRect textureRect)
        {
            return textureRect;
        }

        foreach (Node child in root.GetChildren())
        {
            TextureRect? nested = FindFirstTextureRect(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }
}
