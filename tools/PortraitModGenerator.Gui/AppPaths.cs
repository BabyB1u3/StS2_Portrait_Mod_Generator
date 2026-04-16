namespace PortraitModGenerator.Gui;

internal static class AppPaths
{
    private static readonly string[] RootMarkers =
    [
        Path.Combine("templates", "PortraitReplacementTemplate", "template.json"),
        Path.Combine("data", "official_card_index.json")
    ];

    private static readonly Lazy<string> AppRootValue = new(ResolveAppRoot);

    public static string AppRoot => AppRootValue.Value;

    public static string TemplatesRoot => Path.Combine(AppRoot, "templates");

    public static string PortraitTemplateDirectory => Path.Combine(TemplatesRoot, "PortraitReplacementTemplate");

    public static string OfficialCardIndexPath => Path.Combine(AppRoot, "data", "official_card_index.json");

    public static string CacheRoot => Path.Combine(AppRoot, "cache");

    public static string GeneratedRoot => Path.Combine(AppRoot, "generated");

    public static string GdreToolsPath => ResolveExistingFile(
        Path.Combine(AppRoot, "tools", "gdre", "gdre_tools.exe"),
        Path.Combine(AppRoot, "gdre", "gdre_tools.exe"));

    private static string ResolveAppRoot()
    {
        foreach (string start in GetCandidateStartDirectories())
        {
            string? root = FindRootFrom(start);
            if (root is not null)
            {
                return root;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static IEnumerable<string> GetCandidateStartDirectories()
    {
        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();
    }

    private static string? FindRootFrom(string startDirectory)
    {
        DirectoryInfo? current = new(Path.GetFullPath(startDirectory));
        while (current is not null)
        {
            if (RootMarkers.All(marker => File.Exists(Path.Combine(current.FullName, marker))))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string ResolveExistingFile(params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }
}
