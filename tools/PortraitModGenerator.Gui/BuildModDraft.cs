namespace PortraitModGenerator.Gui;

internal sealed class BuildModDraft
{
    public string ModId { get; set; } = string.Empty;

    public string ModName { get; set; } = string.Empty;

    public string Author { get; set; } = "Unknown Author";

    public string Description { get; set; } = "Generated portrait replacement mod";

    public string ArtifactOutputParent { get; set; } = AppPaths.ArtifactOutputRoot;
}
