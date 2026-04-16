using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Services;

string? command = null;
string[] commandArgs = args;
if (args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal))
{
    command = args[0];
    commandArgs = args[1..];
}

Dictionary<string, string> arguments;

try
{
    arguments = ParseArguments(commandArgs);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    PrintUsage(command);
    return 1;
}

if (args.Length == 0 || arguments.ContainsKey("--help"))
{
    PrintUsage(command);
    return 0;
}

try
{
    switch (command)
    {
        case null:
        case "generate-template":
            RunTemplateGeneration(arguments);
            return 0;
        case "import-pck":
            RunPckImport(arguments);
            return 0;
        case "scan-assets":
            RunAssetScan(arguments);
            return 0;
        default:
            throw new ArgumentException($"Unknown command '{command}'.");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Operation failed: {ex.Message}");
    return 1;
}

static void RunTemplateGeneration(IReadOnlyDictionary<string, string> arguments)
{
    TemplateProjectGenerator generator = new();
    TemplateGenerationRequest request = new()
    {
        TemplateDirectory = GetRequired(arguments, "--template"),
        OutputDirectory = GetRequired(arguments, "--output"),
        OverwriteExistingOutput = arguments.ContainsKey("--overwrite"),
        TokenValues = BuildTokenValues(arguments)
    };

    TemplateGenerationResult result = generator.Generate(request);

    Console.WriteLine("Template generation completed.");
    Console.WriteLine($"Template: {result.TemplateId} v{result.TemplateVersion}");
    Console.WriteLine($"Output: {result.OutputDirectory}");
    Console.WriteLine($"Project: {result.EntryProjectPath}");
    Console.WriteLine($"Manifest: {result.ManifestPath}");
}

static void RunPckImport(IReadOnlyDictionary<string, string> arguments)
{
    string currentDirectory = Directory.GetCurrentDirectory();
    string defaultGdrePath = Path.Combine(currentDirectory, "gdre", "gdre_tools.exe");

    GdrePckImporter importer = new();
    PckImportRequest request = new()
    {
        SourcePckPath = GetRequired(arguments, "--pck"),
        OutputDirectory = GetRequired(arguments, "--output"),
        GdreToolsPath = GetOptional(arguments, "--gdre", defaultGdrePath),
        LogFilePath = GetOptional(arguments, "--log", string.Empty),
        OverwriteOutput = arguments.ContainsKey("--overwrite")
    };

    PckImportResult result = importer.Import(request);

    Console.WriteLine("PCK import completed.");
    Console.WriteLine($"Source: {result.SourcePckPath}");
    Console.WriteLine($"Output: {result.ExtractRoot}");
    Console.WriteLine($"GDRE: {result.GdreToolsPath}");
    Console.WriteLine($"ExitCode: {result.ExitCode}");
    Console.WriteLine($"Log: {result.LogFilePath}");
}

static void RunAssetScan(IReadOnlyDictionary<string, string> arguments)
{
    AssetScanner scanner = new();
    AssetScanRequest request = new()
    {
        InputDirectory = GetRequired(arguments, "--input"),
        OutputJsonPath = GetOptional(arguments, "--output-json", string.Empty)
    };

    AssetScanResult result = scanner.Scan(request);

    Console.WriteLine("Asset scan completed.");
    Console.WriteLine($"Input: {result.InputDirectory}");
    Console.WriteLine($"ScannedFiles: {result.TotalFilesScanned}");
    Console.WriteLine($"ImageFiles: {result.ImageFilesFound}");
    Console.WriteLine($"OutputJson: {result.OutputJsonPath}");
}

static Dictionary<string, string> ParseArguments(string[] args)
{
    Dictionary<string, string> parsed = new(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < args.Length; i++)
    {
        string current = args[i];
        if (!current.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected argument '{current}'.");
        }

        if (string.Equals(current, "--overwrite", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(current, "--help", StringComparison.OrdinalIgnoreCase))
        {
            parsed[current] = "true";
            continue;
        }

        string key = current;
        string? inlineValue = null;
        int separatorIndex = current.IndexOf('=');
        if (separatorIndex > 0)
        {
            key = current[..separatorIndex];
            inlineValue = current[(separatorIndex + 1)..];
        }

        if (inlineValue is not null)
        {
            parsed[key] = inlineValue;
            continue;
        }

        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for argument '{current}'.");
        }

        parsed[key] = args[++i];
    }

    return parsed;
}

static IReadOnlyDictionary<string, string> BuildTokenValues(IReadOnlyDictionary<string, string> arguments)
{
    string modId = GetRequired(arguments, "--mod-id");

    return new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["__MOD_ID__"] = modId,
        ["__MOD_NAME__"] = GetOptional(arguments, "--mod-name", modId),
        ["__AUTHOR__"] = GetOptional(arguments, "--author", "Unknown Author"),
        ["__DESCRIPTION__"] = GetOptional(arguments, "--description", "Generated portrait replacement mod"),
        ["__VERSION__"] = GetOptional(arguments, "--version", "v0.1.0")
    };
}

static string GetRequired(IReadOnlyDictionary<string, string> arguments, string key)
{
    if (!arguments.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
    {
        throw new ArgumentException($"Missing required argument '{key}'.");
    }

    return value;
}

static string GetOptional(IReadOnlyDictionary<string, string> arguments, string key, string fallback)
{
    return arguments.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : fallback;
}

static void PrintUsage(string? command)
{
    switch (command)
    {
        case "import-pck":
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project tools/PortraitModGenerator.Cli -- import-pck \\");
            Console.WriteLine("    --pck <input.pck> \\");
            Console.WriteLine("    --output <extractDir> [options]");
            Console.WriteLine();
            Console.WriteLine("Required:");
            Console.WriteLine("  --pck            Path to the source PCK file");
            Console.WriteLine("  --output         Directory where extracted files will be written");
            Console.WriteLine();
            Console.WriteLine("Optional:");
            Console.WriteLine("  --gdre           Path to gdre_tools.exe");
            Console.WriteLine("  --log            Explicit log file path");
            Console.WriteLine("  --overwrite      Allow using an existing output directory");
            Console.WriteLine("  --help           Show this help");
            break;
        case "scan-assets":
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project tools/PortraitModGenerator.Cli -- scan-assets \\");
            Console.WriteLine("    --input <recoverDir> [options]");
            Console.WriteLine();
            Console.WriteLine("Required:");
            Console.WriteLine("  --input          Directory to scan for extracted image assets");
            Console.WriteLine();
            Console.WriteLine("Optional:");
            Console.WriteLine("  --output-json    Explicit output JSON path");
            Console.WriteLine("  --help           Show this help");
            break;
        case null:
        case "generate-template":
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project tools/PortraitModGenerator.Cli -- generate-template \\");
            Console.WriteLine("    --template <templateDir> \\");
            Console.WriteLine("    --output <outputDir> \\");
            Console.WriteLine("    --mod-id <modId> [options]");
            Console.WriteLine();
            Console.WriteLine("Required:");
            Console.WriteLine("  --template       Path to a template directory containing template.json");
            Console.WriteLine("  --output         Output directory for the generated mod project");
            Console.WriteLine("  --mod-id         Mod identifier used for project, manifest and resource paths");
            Console.WriteLine();
            Console.WriteLine("Optional:");
            Console.WriteLine("  --mod-name       Display name for the generated mod");
            Console.WriteLine("  --author         Author name");
            Console.WriteLine("  --description    Mod description");
            Console.WriteLine("  --version        Mod version (default: v0.1.0)");
            Console.WriteLine("  --overwrite      Allow using an existing output directory");
            Console.WriteLine("  --help           Show this help");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  generate-template   Generate a mod project from a template");
            Console.WriteLine("  import-pck          Extract a PCK with GDRETools");
            Console.WriteLine("  scan-assets         Scan a recovered directory for image assets");
            break;
        default:
            Console.WriteLine($"Unknown command '{command}'.");
            Console.WriteLine("Available commands: generate-template, import-pck, scan-assets");
            break;
    }
}
