using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace __MOD_ID__.__MOD_ID__Code;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "__MOD_ID__";

    public static void Initialize()
    {
        PortraitReplacementRegistry.EnsureLoaded();
        FrameReplacementRegistry.EnsureLoaded();

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        GD.Print($"[{ModId}] initialized.");
    }
}
