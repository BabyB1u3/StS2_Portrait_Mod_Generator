using Godot;
using MegaCrit.Sts2.Core.Models;

namespace RegentFemCards.RegentFemCardsCode;

internal static class PortraitReplacementRegistry
{
    private static readonly Dictionary<string, string> PortraitPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arsenal"] = "res://RegentFemCards/CardPortraits/Regent/Arsenal.png",
        ["Begone"] = "res://RegentFemCards/CardPortraits/Regent/Begone.png",
        ["Bulwark"] = "res://RegentFemCards/CardPortraits/Regent/Bulwark.png",
        ["Charge"] = "res://RegentFemCards/CardPortraits/Regent/Charge.png",
        ["ChildOfTheStars"] = "res://RegentFemCards/CardPortraits/Regent/ChildOfTheStars.png",
        ["CloakOfStars"] = "res://RegentFemCards/CardPortraits/Regent/CloakOfStars.png",
        ["Conqueror"] = "res://RegentFemCards/CardPortraits/Regent/Conqueror.png",
        ["Convergence"] = "res://RegentFemCards/CardPortraits/Regent/Convergence.png",
        ["CosmicIndifference"] = "res://RegentFemCards/CardPortraits/Regent/CosmicIndifference.png",
        ["CrashLanding"] = "res://RegentFemCards/CardPortraits/Regent/CrashLanding.png",
        ["CrushUnder"] = "res://RegentFemCards/CardPortraits/Regent/CrushUnder.png",
        ["DecisionsDecisions"] = "res://RegentFemCards/CardPortraits/Regent/DecisionsDecisions.png",
        ["DefendRegent"] = "res://RegentFemCards/CardPortraits/Regent/DefendRegent.png",
        ["FallingStar"] = "res://RegentFemCards/CardPortraits/Regent/FallingStar.png",
        ["GammaBlast"] = "res://RegentFemCards/CardPortraits/Regent/GammaBlast.png",
        ["Genesis"] = "res://RegentFemCards/CardPortraits/Regent/Genesis.png",
        ["Glow"] = "res://RegentFemCards/CardPortraits/Regent/Glow.png",
        ["HeavenlyDrill"] = "res://RegentFemCards/CardPortraits/Regent/HeavenlyDrill.png",
        ["KinglyKick"] = "res://RegentFemCards/CardPortraits/Regent/KinglyKick.png",
        ["KinglyPunch"] = "res://RegentFemCards/CardPortraits/Regent/KinglyPunch.png",
        ["MakeItSo"] = "res://RegentFemCards/CardPortraits/Regent/MakeItSo.png",
        ["ManifestAuthority"] = "res://RegentFemCards/CardPortraits/Regent/MenifestAuthority.png",
        ["MonarchsGaze"] = "res://RegentFemCards/CardPortraits/Regent/MonarchsGaze.png",
        ["NeutronAegis"] = "res://RegentFemCards/CardPortraits/Regent/NeutronAegis.png",
        ["Parry"] = "res://RegentFemCards/CardPortraits/Regent/Parry.png",
        ["Patter"] = "res://RegentFemCards/CardPortraits/Regent/Patter.png",
        ["Prophesize"] = "res://RegentFemCards/CardPortraits/Regent/Prophesize.png",
        ["Quasar"] = "res://RegentFemCards/CardPortraits/Regent/Quasar.png",
        ["Radiate"] = "res://RegentFemCards/CardPortraits/Regent/Radiate.png",
        ["RefineBlade"] = "res://RegentFemCards/CardPortraits/Regent/RefineBlade.png",
        ["RoyalGamble"] = "res://RegentFemCards/CardPortraits/Regent/RoyalGamble.png",
        ["SovereignBlade"] = "res://RegentFemCards/CardPortraits/Regent/SovereignBlade.png",
        ["SwordSage"] = "res://RegentFemCards/CardPortraits/Regent/SwordSage.png",
        ["Terraforming"] = "res://RegentFemCards/CardPortraits/Regent/Terraforming.png",
        ["TheSmith"] = "res://RegentFemCards/CardPortraits/Regent/TheSmith.png",
        ["Tyranny"] = "res://RegentFemCards/CardPortraits/Regent/Tyranny.png",
        ["Venerate"] = "res://RegentFemCards/CardPortraits/Regent/Venerate.png",
        ["VoidForm"] = "res://RegentFemCards/CardPortraits/Regent/VoidForm.png",
        ["WroughtInWar"] = "res://RegentFemCards/CardPortraits/Regent/WroughtInWar.png",
    };

    public static void EnsureLoaded()
    {
        GD.Print($"[RegentFemCards] Registered {PortraitPaths.Count} portrait replacement(s).");
    }

    public static bool TryGetPath(CardModel model, out string? path)
    {
        path = null;

        string cardName = model.GetType().Name;
        if (!PortraitPaths.TryGetValue(cardName, out string? configuredPath))
        {
            return false;
        }

        if (!ResourceLoader.Exists(configuredPath))
        {
            GD.PushWarning($"[RegentFemCards] Portrait file not found for {cardName}: {configuredPath}");
            return false;
        }

        path = configuredPath;
        return true;
    }
}
