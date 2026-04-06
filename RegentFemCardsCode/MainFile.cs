using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RegentFemCards.RegentFemCardsCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "RegentFemCards";

    public static void Initialize()
    {
        PortraitReplacementRegistry.EnsureLoaded();
        FrameReplacementRegistry.EnsureLoaded();

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        GD.Print($"[{ModId}] initialized.");
    }
}
