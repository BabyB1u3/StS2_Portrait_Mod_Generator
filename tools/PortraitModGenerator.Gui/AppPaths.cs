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

    public static string ToolsRoot => Path.Combine(AppRoot, "tools");

    public static string TemplatesRoot => Path.Combine(AppRoot, "templates");

    public static string PortraitTemplateDirectory => Path.Combine(TemplatesRoot, "PortraitReplacementTemplate");

    public static string OfficialCardIndexPath => Path.Combine(AppRoot, "data", "official_card_index.json");

    public static string OfficialCardPortraitsRoot => Path.Combine(AppRoot, "data", "official_card_portraits");

    public static string PackagesRoot => Path.Combine(AppRoot, "packages");

    public static string CacheRoot => Path.Combine(AppRoot, "cache");

    public static string ArtifactOutputRoot => Path.Combine(AppRoot, "artifacts");

    public static string GeneratedRoot => Path.Combine(AppRoot, "generated");

    public static string LogsRoot => Path.Combine(AppRoot, "logs");

    public static string DotnetCliHome => Path.Combine(CacheRoot, ".dotnet_cli");

    public static string DotnetExecutablePath => ResolveExistingFileOrFallback(
        "dotnet",
        Path.Combine(ToolsRoot, "dotnet", "dotnet.exe"));

    public static string NuGetConfigPath => ResolveExistingFile(
        Path.Combine(AppRoot, "config", "NuGet.config"),
        Path.Combine(AppRoot, "nuget.config"));

    public static string GdreToolsPath => ResolveExistingFile(
        Path.Combine(ToolsRoot, "gdre", "gdre_tools.exe"),
        Path.Combine(AppRoot, "gdre", "gdre_tools.exe"));

    public static string GodotExecutablePath => ResolveExistingFile(
        ResolveBundledGodotExecutablePath(),
        Environment.GetEnvironmentVariable("PORTRAIT_MOD_GENERATOR_GODOT"),
        @"C:\megadot\MegaDot_v4.5.1-stable_mono_win64.exe");

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

    private static string ResolveExistingFile(params string?[] candidates)
    {
        foreach (string? candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates.First(candidate => !string.IsNullOrWhiteSpace(candidate))!;
    }

    private static string ResolveExistingFileOrFallback(string fallback, params string?[] candidates)
    {
        foreach (string? candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return fallback;
    }

    private static string? ResolveBundledGodotExecutablePath()
    {
        string[] godotRoots =
        [
            Path.Combine(ToolsRoot, "godot"),
            Path.Combine(AppRoot, "ReleaseInput", "godot")
        ];

        foreach (string godotRoot in godotRoots)
        {
            if (!Directory.Exists(godotRoot))
            {
                continue;
            }

            string[] preferredCandidates =
            [
                Path.Combine(godotRoot, "MegaDot_v4.5.1-stable_mono_win64.exe"),
                Path.Combine(godotRoot, "Godot_v4.5.1-stable_mono_win64.exe"),
                Path.Combine(godotRoot, "godot.exe")
            ];

            foreach (string candidate in preferredCandidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            string? fallback = Directory.EnumerateFiles(godotRoot, "*.exe", SearchOption.AllDirectories)
                .OrderBy(path => GetGodotCandidateOrder(Path.GetFileName(path)))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (fallback is not null)
            {
                return fallback;
            }
        }

        return null;
    }

    private static int GetGodotCandidateOrder(string fileName)
    {
        if (fileName.Contains("megadot", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (fileName.Contains("mono", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (fileName.Contains("godot", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        return 3;
    }
}
