using System;
using System.IO;
using Deskmate.Infrastructure.Avatars;

namespace Deskmate.Core.Tests;

public class AvatarPackLoaderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "deskmate-packs-" + Guid.NewGuid().ToString("N"));

    public AvatarPackLoaderTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static string RepoAvatarsDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "avatars", "mint")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("avatars folder not found"), "avatars");
    }

    // A minimal 1x1 PNG header is enough: the loader only reads the size from the IHDR chunk.
    private static byte[] PngHeader(int width, int height)
    {
        var bytes = new byte[24];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, (byte)'I', (byte)'H', (byte)'D', (byte)'R' }.CopyTo(bytes, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20), height);
        return bytes;
    }

    private void WritePack(string name, string manifest, int sheetWidth = 200, int sheetHeight = 50)
    {
        var directory = Directory.CreateDirectory(Path.Combine(_root, name)).FullName;
        File.WriteAllText(Path.Combine(directory, "avatar.json"), manifest);
        File.WriteAllBytes(Path.Combine(directory, "idle.png"), PngHeader(sheetWidth, sheetHeight));
    }

    private const string GoodManifest = """
        { "name": "T", "frameSize": { "width": 50, "height": 50 },
          "animations": { "idle": { "sheet": "idle.png", "frames": 4, "fps": 4, "loop": true } } }
        """;

    [Theory]
    [InlineData("mint")]
    [InlineData("slate")]
    public void ShippedPacks_AreValid(string packName)
    {
        var loader = new AvatarPackLoader(RepoAvatarsDirectory());

        var pack = loader.Load(packName);

        Assert.Equal(AvatarPackValidatorKnown.Count, AvatarPackValidatorKnown.CountPresent(pack));
    }

    [Fact]
    public void Load_ValidPack_Succeeds()
    {
        WritePack("good", GoodManifest);

        var pack = new AvatarPackLoader(_root).Load("good");

        Assert.Equal("T", pack.Name);
    }

    [Fact]
    public void Load_MissingManifest_ThrowsWithClearMessage()
    {
        Directory.CreateDirectory(Path.Combine(_root, "empty"));

        var ex = Assert.Throws<InvalidAvatarPackException>(() => new AvatarPackLoader(_root).Load("empty"));

        Assert.Contains("avatar.json", ex.Message);
    }

    [Fact]
    public void Load_BadJson_Throws()
    {
        WritePack("broken", "{ not json");

        var ex = Assert.Throws<InvalidAvatarPackException>(() => new AvatarPackLoader(_root).Load("broken"));

        Assert.Contains("not valid JSON", ex.Message);
    }

    [Fact]
    public void Load_WrongSheetSize_ReportsTheMismatch()
    {
        WritePack("small", GoodManifest, sheetWidth: 100);

        var ex = Assert.Throws<InvalidAvatarPackException>(() => new AvatarPackLoader(_root).Load("small"));

        Assert.Contains("200x50", ex.Message);
    }

    [Fact]
    public void ListValidPacks_SkipsBrokenOnes()
    {
        WritePack("good", GoodManifest);
        WritePack("broken", "{ not json");

        Assert.Equal(["good"], new AvatarPackLoader(_root).ListValidPacks());
    }
}

/// <summary>Every animation the app can play should be present in the packs we ship.</summary>
internal static class AvatarPackValidatorKnown
{
    public static int Count => AvatarPackValidator.KnownAnimations.Count;

    public static int CountPresent(Deskmate.Core.Models.AvatarPack pack)
    {
        var present = 0;
        foreach (var name in AvatarPackValidator.KnownAnimations)
        {
            if (pack.Animations.ContainsKey(name))
            {
                present++;
            }
        }

        return present;
    }
}
