using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Deskmate.Core;
using Deskmate.Core.Models;

namespace Deskmate.Infrastructure.Avatars;

/// <summary>
/// Reads an avatar pack's `avatar.json` manifest. Packs live as folders
/// under an `avatars` directory shipped alongside the app. Every pack is validated
/// on load (see <see cref="AvatarPackValidator"/>), so a bad pack fails with a clear
/// <see cref="InvalidAvatarPackException"/> instead of a crash mid-animation.
/// </summary>
public class AvatarPackLoader(string? packsDirectory = null)
{
    private readonly string _packsDirectory = packsDirectory ?? GetPacksDirectory();

    public const string FallbackPackName = "mint";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static string GetPacksDirectory() => Path.Combine(AppContext.BaseDirectory, "avatars");

    public AvatarPack Load(string packName)
    {
        var packDirectory = Path.Combine(_packsDirectory, packName);
        var manifestPath = Path.Combine(packDirectory, "avatar.json");

        if (!File.Exists(manifestPath))
        {
            throw new InvalidAvatarPackException(packName, ["'avatar.json' was not found."]);
        }

        AvatarPack? pack;
        try
        {
            pack = JsonSerializer.Deserialize<AvatarPack>(File.ReadAllText(manifestPath), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidAvatarPackException(packName, [$"'avatar.json' is not valid JSON: {ex.Message}"]);
        }

        if (pack is null)
        {
            throw new InvalidAvatarPackException(packName, ["'avatar.json' is empty."]);
        }

        var problems = AvatarPackValidator.Validate(pack, sheet => ReadPngSize(Path.Combine(packDirectory, sheet)));
        if (problems.Count > 0)
        {
            throw new InvalidAvatarPackException(packName, problems);
        }

        return pack;
    }

    /// <summary>Whether the named pack loads cleanly. Used to hide broken packs from the picker.</summary>
    public bool TryLoad(string packName, out AvatarPack? pack, out InvalidAvatarPackException? error)
    {
        try
        {
            pack = Load(packName);
            error = null;
            return true;
        }
        catch (InvalidAvatarPackException ex)
        {
            pack = null;
            error = ex;
            return false;
        }
    }

    /// <summary>Names of the pack folders that load cleanly, sorted.</summary>
    public string[] ListValidPacks()
    {
        var directory = _packsDirectory;
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var names = Directory.GetDirectories(directory);
        var valid = new List<string>();
        foreach (var path in names)
        {
            var name = Path.GetFileName(path);
            if (TryLoad(name, out _, out _))
            {
                valid.Add(name);
            }
        }

        valid.Sort(StringComparer.OrdinalIgnoreCase);
        return valid.ToArray();
    }

    /// <summary>Reads a PNG's pixel size from its header, without decoding the image. Null if missing or not a PNG.</summary>
    private static (int Width, int Height)? ReadPngSize(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[24];
            if (stream.Read(header) < 24 || !header[..8].SequenceEqual(PngSignature))
            {
                return null;
            }

            var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
            var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
            return (width, height);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
