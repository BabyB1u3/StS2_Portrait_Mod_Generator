using System.Text;
using System.Text.RegularExpressions;

namespace PortraitModGenerator.Core.Services;

public sealed class NameCanonicalizer
{
    private static readonly Regex CamelBoundaryRegex = new("([a-z0-9])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex SeparatorRegex = new("[^A-Za-z0-9]+", RegexOptions.Compiled);
    private static readonly HashSet<string> NoiseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "portrait",
        "portraits",
        "art",
        "arts",
        "image",
        "images",
        "illustration",
        "illustrations",
        "card",
        "cards",
        "full",
        "final",
        "mod"
    };

    private static readonly HashSet<string> IgnoredExactNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "mod_image",
        "modimage"
    };

    public CanonicalizedName Canonicalize(string fileName)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            return new CanonicalizedName([], true, "File name stem is empty.");
        }

        string lowerStem = stem.ToLowerInvariant();
        if (IgnoredExactNames.Contains(lowerStem))
        {
            return new CanonicalizedName([], true, "Ignored known non-card image.");
        }

        List<string> rawTokens = Tokenize(stem);
        if (rawTokens.Count == 0)
        {
            return new CanonicalizedName([], true, "No usable tokens found in file name.");
        }

        List<string> cleanedTokens = rawTokens
            .Where(token => !NoiseTokens.Contains(token))
            .ToList();

        if (cleanedTokens.Count == 0)
        {
            return new CanonicalizedName([], true, "Only noise tokens remained after cleanup.");
        }

        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase)
        {
            lowerStem
        };

        AddKey(keys, ToSnakeCase(rawTokens));
        AddKey(keys, ToSnakeCase(cleanedTokens));
        AddKey(keys, ToPascalCase(rawTokens));
        AddKey(keys, ToPascalCase(cleanedTokens));

        for (int start = 0; start < cleanedTokens.Count; start++)
        {
            for (int length = 1; length <= cleanedTokens.Count - start; length++)
            {
                if (start == 0 && length == cleanedTokens.Count)
                {
                    continue;
                }

                List<string> spanTokens = cleanedTokens.GetRange(start, length);
                AddKey(keys, ToSnakeCase(spanTokens));
                AddKey(keys, ToPascalCase(spanTokens));
            }
        }

        return new CanonicalizedName(keys.ToList(), false, null);
    }

    private static void AddKey(HashSet<string> keys, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            keys.Add(value);
        }
    }

    private static List<string> Tokenize(string value)
    {
        string withBoundaries = CamelBoundaryRegex.Replace(value, "$1 $2");
        string normalized = SeparatorRegex.Replace(withBoundaries, " ");
        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().ToLowerInvariant())
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .ToList();
    }

    private static string ToSnakeCase(IReadOnlyList<string> tokens)
    {
        return string.Join("_", tokens);
    }

    private static string ToPascalCase(IReadOnlyList<string> tokens)
    {
        StringBuilder builder = new();
        foreach (string token in tokens)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            builder.Append(char.ToUpperInvariant(token[0]));
            if (token.Length > 1)
            {
                builder.Append(token[1..]);
            }
        }

        return builder.ToString();
    }

    public sealed record CanonicalizedName(IReadOnlyList<string> Keys, bool Ignored, string? IgnoredReason);
}
