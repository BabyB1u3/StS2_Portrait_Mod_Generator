using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RegentFemCards.RegentFemCardsCode;

[HarmonyPatch]
internal static class CardPortraitReplacementPatch
{
    private static readonly BindingFlags InstanceBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static bool _loggedTarget;

    private static MethodBase? TargetMethod()
    {
        Type cardModelType = typeof(CardModel);

        string[] preferredPropertyNames =
        [
            "PortraitPath",
            "Portrait",
            "CardArtPath",
            "CardArt"
        ];

        foreach (string propertyName in preferredPropertyNames)
        {
            PropertyInfo? property = cardModelType.GetProperty(propertyName, InstanceBindings);
            if (property?.PropertyType == typeof(string) && property.GetMethod is not null)
            {
                LogTarget(property.GetMethod);
                return property.GetMethod;
            }
        }

        foreach (PropertyInfo property in cardModelType.GetProperties(InstanceBindings))
        {
            if (property.PropertyType == typeof(string) &&
                property.GetMethod is not null &&
                IsPortraitLikeName(property.Name))
            {
                LogTarget(property.GetMethod);
                return property.GetMethod;
            }
        }

        foreach (MethodInfo method in cardModelType.GetMethods(InstanceBindings))
        {
            if (method.ReturnType == typeof(string) &&
                method.GetParameters().Length == 0 &&
                IsPortraitLikeName(method.Name))
            {
                LogTarget(method);
                return method;
            }
        }

        GD.PushWarning("[RegentFemCards] Could not locate a portrait-path getter on CardModel. Falling back is required.");
        return null;
    }

    private static bool IsPortraitLikeName(string name)
    {
        return name.Contains("Portrait", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("CardArt", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("ArtPath", StringComparison.OrdinalIgnoreCase);
    }

    private static void LogTarget(MethodBase target)
    {
        if (_loggedTarget)
        {
            return;
        }

        _loggedTarget = true;
        GD.Print($"[RegentFemCards] Patching portrait getter: {target.DeclaringType?.FullName}.{target.Name}");
    }

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
