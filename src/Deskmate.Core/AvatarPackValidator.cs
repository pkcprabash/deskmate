using System;
using System.Collections.Generic;
using Deskmate.Core.Models;

namespace Deskmate.Core;

/// <summary>
/// Checks an avatar pack manifest for mistakes a pack author is likely to make and reports all
/// of them at once, in plain language. No file-system or image dependency: the caller supplies a
/// lookup for each sprite sheet's pixel size (null when the file is missing), so this stays
/// fully unit-testable.
/// </summary>
public static class AvatarPackValidator
{
    /// <summary>Animations the app plays. Only <c>idle</c> is mandatory; a missing one just isn't shown.</summary>
    public static readonly IReadOnlyList<string> KnownAnimations =
        ["idle", "wave", "held", "typing", "stretch", "coffee", "look", "sleeping", "yawn", "sign"];

    public const string RequiredAnimation = "idle";

    public static IReadOnlyList<string> Validate(AvatarPack pack, Func<string, (int Width, int Height)?> getSheetSize)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(pack.Name))
        {
            errors.Add("'name' is missing.");
        }

        var frameSizeValid = pack.FrameSize.Width > 0 && pack.FrameSize.Height > 0;
        if (!frameSizeValid)
        {
            errors.Add("'frameSize' needs a positive width and height.");
        }

        if (!pack.Animations.ContainsKey(RequiredAnimation))
        {
            errors.Add($"The '{RequiredAnimation}' animation is required.");
        }

        foreach (var (name, animation) in pack.Animations)
        {
            ValidateAnimation(name, animation, pack.FrameSize, frameSizeValid, getSheetSize, errors);
        }

        return errors;
    }

    private static void ValidateAnimation(
        string name,
        AvatarAnimation animation,
        AvatarFrameSize frameSize,
        bool frameSizeValid,
        Func<string, (int Width, int Height)?> getSheetSize,
        List<string> errors)
    {
        if (animation.Frames < 1)
        {
            errors.Add($"Animation '{name}': 'frames' must be at least 1.");
        }

        if (animation.Fps < 1)
        {
            errors.Add($"Animation '{name}': 'fps' must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(animation.Sheet))
        {
            errors.Add($"Animation '{name}': 'sheet' is missing.");
            return;
        }

        if (animation.Sheet.Contains("..") || animation.Sheet.Contains('/') || animation.Sheet.Contains('\\'))
        {
            errors.Add($"Animation '{name}': 'sheet' must be a file name inside the pack folder, not a path.");
            return;
        }

        if (getSheetSize(animation.Sheet) is not { } size)
        {
            errors.Add($"Animation '{name}': sprite sheet '{animation.Sheet}' was not found or is not a PNG.");
            return;
        }

        if (frameSizeValid && animation.Frames >= 1)
        {
            var expectedWidth = frameSize.Width * animation.Frames;
            if (size.Width != expectedWidth || size.Height != frameSize.Height)
            {
                errors.Add(
                    $"Animation '{name}': '{animation.Sheet}' is {size.Width}x{size.Height}, but {animation.Frames} frame(s) of " +
                    $"{frameSize.Width}x{frameSize.Height} need {expectedWidth}x{frameSize.Height}.");
            }
        }
    }
}
