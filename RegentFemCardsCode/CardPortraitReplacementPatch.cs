using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RegentFemCards.RegentFemCardsCode;

[HarmonyPatch]
internal static class CardPortraitReplacementPatch
{
    private static readonly BindingFlags InstanceBindings =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly string[] PreferredPropertyNames =
    [
        "PortraitPath",
        "Portrait",
        "CardArtPath",
        "CardArt"
    ];

    private static readonly string[] PreferredMethodNames =
    [
        "GetPortraitPath",
        "GetPortrait",
        "GetCardArtPath",
        "GetCardArt"
    ];

    private static bool _loggedTargets;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        HashSet<MethodBase> targets = [];

        AddTargetsFromType(targets, typeof(CardModel));

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
            }
            catch
            {
                continue;
            }

            foreach (Type type in types)
            {
                if (!typeof(CardModel).IsAssignableFrom(type) || type == typeof(CardModel))
                {
                    continue;
                }

                AddTargetsFromType(targets, type);
            }
        }

        if (!_loggedTargets)
        {
            _loggedTargets = true;
            if (targets.Count == 0)
            {
                GD.PushWarning("[RegentFemCards] No explicit portrait getter target was found. Portrait replacement patch will be inactive.");
            }
            else
            {
                foreach (MethodBase target in targets.OrderBy(static method => method.DeclaringType?.FullName).ThenBy(static method => method.Name))
                {
                    GD.Print($"[RegentFemCards] Patching portrait getter: {target.DeclaringType?.FullName}.{target.Name}");
                }
            }
        }

        return targets;
    }

    private static void AddTargetsFromType(ISet<MethodBase> targets, Type type)
    {
        foreach (string propertyName in PreferredPropertyNames)
        {
            PropertyInfo? property = type.GetProperty(propertyName, InstanceBindings);
            if (property?.PropertyType == typeof(string) && property.GetMethod is not null)
            {
                targets.Add(property.GetMethod);
            }
        }

        foreach (string methodName in PreferredMethodNames)
        {
            MethodInfo? method = type.GetMethod(methodName, InstanceBindings, null, Type.EmptyTypes, null);
            if (method?.ReturnType == typeof(string))
            {
                targets.Add(method);
            }
        }
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
