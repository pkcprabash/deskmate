using System;
using System.IO;
using System.Text.Json;
using Deskmate.Core.Models;

namespace Deskmate.Infrastructure.Avatars;

/// <summary>
/// Reads an avatar pack's `avatar.json` manifest. Packs live as folders
/// under an `avatars` directory shipped alongside the app.
/// </summary>
public class AvatarPackLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static string GetPacksDirectory() => Path.Combine(AppContext.BaseDirectory, "avatars");

    public AvatarPack Load(string packName)
    {
        var manifestPath = Path.Combine(GetPacksDirectory(), packName, "avatar.json");
        var json = File.ReadAllText(manifestPath);

        return JsonSerializer.Deserialize<AvatarPack>(json, JsonOptions)
            ?? throw new InvalidDataException($"Avatar pack '{packName}' manifest is empty or invalid.");
    }
}
