using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia.Media.Imaging;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Avatars;

namespace Deskmate.App.Avatars;

/// <summary>
/// An <see cref="AvatarPack"/> manifest paired with lazily-loaded sprite
/// sheet bitmaps. Bitmap loading is Avalonia-specific, so it lives here
/// rather than in Deskmate.Infrastructure's framework-agnostic loader.
/// </summary>
public class LoadedAvatarPack
{
    private readonly AvatarPack _pack;
    private readonly string _packDirectory;
    private readonly Dictionary<string, Bitmap> _sheets = new();

    private LoadedAvatarPack(AvatarPack pack, string packDirectory)
    {
        _pack = pack;
        _packDirectory = packDirectory;
    }

    public AvatarFrameSize FrameSize => _pack.FrameSize;

    public static LoadedAvatarPack Load(AvatarPackLoader loader, string packName)
    {
        var pack = loader.Load(packName);
        var directory = Path.Combine(AvatarPackLoader.GetPacksDirectory(), packName);
        return new LoadedAvatarPack(pack, directory);
    }

    /// <summary>
    /// Loads the named pack, or the built-in fallback if it is missing or invalid, so a bad
    /// or deleted pack never stops the avatar from appearing.
    /// </summary>
    public static LoadedAvatarPack LoadWithFallback(AvatarPackLoader loader, string packName)
    {
        try
        {
            return Load(loader, packName);
        }
        catch (InvalidAvatarPackException ex)
        {
            Trace.TraceWarning($"{ex.Message}{Environment.NewLine}Falling back to '{AvatarPackLoader.FallbackPackName}'.");
            return Load(loader, AvatarPackLoader.FallbackPackName);
        }
    }

    public bool TryGetAnimation(string name, out AvatarAnimation animation, out Bitmap sheet)
    {
        if (!_pack.Animations.TryGetValue(name, out var foundAnimation))
        {
            animation = default!;
            sheet = default!;
            return false;
        }

        animation = foundAnimation;

        if (!_sheets.TryGetValue(name, out var foundSheet))
        {
            foundSheet = new Bitmap(Path.Combine(_packDirectory, animation.Sheet));
            _sheets[name] = foundSheet;
        }

        sheet = foundSheet;
        return true;
    }
}
