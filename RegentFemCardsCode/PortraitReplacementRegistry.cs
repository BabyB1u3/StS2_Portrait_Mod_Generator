using Godot;
using MegaCrit.Sts2.Core.Models;

namespace RegentFemCards.RegentFemCardsCode;

internal static class PortraitReplacementRegistry
{
    private const string PortraitDirectory = "res://RegentFemCards/CardPortraits/Regent";

    private static readonly Dictionary<string, string> PortraitPaths = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Texture2D?> TextureCache = new(StringComparer.OrdinalIgnoreCase);

    private static bool _loaded;

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        PortraitPaths.Clear();

        DirAccess? dir = DirAccess.Open(PortraitDirectory);
        if (dir is null)
        {
            GD.PushWarning($"[RegentFemCards] Portrait directory not found: {PortraitDirectory}");
            return;
        }

        dir.ListDirBegin();
        while (true)
        {
            string fileName = dir.GetNext();
            if (string.IsNullOrEmpty(fileName))
            {
                break;
            }

            if (dir.CurrentIsDir() || !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string cardName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            PortraitPaths[cardName] = $"{PortraitDirectory}/{fileName}";
        }
        dir.ListDirEnd();

        GD.Print($"[RegentFemCards] Loaded {PortraitPaths.Count} portrait replacement(s).");
    }

    public static bool TryGetTexture(CardModel model, out Texture2D? texture)
    {
        EnsureLoaded();

        texture = null;
        string cardName = model.GetType().Name;
        if (!PortraitPaths.TryGetValue(cardName, out string? path))
        {
            return false;
        }

        if (TextureCache.TryGetValue(path, out Texture2D? cachedTexture))
        {
            texture = cachedTexture;
            return texture is not null;
        }

        texture = GD.Load<Texture2D>(path);
        TextureCache[path] = texture;

        if (texture is null)
        {
            GD.PushWarning($"[RegentFemCards] Failed to load portrait texture: {path}");
        }

        return texture is not null;
    }
}
