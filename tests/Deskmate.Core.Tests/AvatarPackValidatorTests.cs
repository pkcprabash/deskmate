using System;
using System.Collections.Generic;
using System.Linq;
using Deskmate.Core;
using Deskmate.Core.Models;

namespace Deskmate.Core.Tests;

public class AvatarPackValidatorTests
{
    private static AvatarPack ValidPack() => new()
    {
        Name = "Test",
        FrameSize = new AvatarFrameSize { Width = 100, Height = 80 },
        Animations = new Dictionary<string, AvatarAnimation>
        {
            ["idle"] = new() { Sheet = "idle.png", Frames = 4, Fps = 4, Loop = true },
        },
    };

    // Every sheet is exactly frames * 100 wide and 80 tall unless a test says otherwise.
    private static Func<string, (int, int)?> Sizes(int width = 400, int height = 80) => _ => (width, height);

    [Fact]
    public void ValidPack_HasNoErrors()
    {
        Assert.Empty(AvatarPackValidator.Validate(ValidPack(), Sizes()));
    }

    [Fact]
    public void MissingName_IsReported()
    {
        var pack = ValidPack();
        pack.Name = " ";

        Assert.Contains(AvatarPackValidator.Validate(pack, Sizes()), e => e.Contains("'name'"));
    }

    [Fact]
    public void NonPositiveFrameSize_IsReported()
    {
        var pack = ValidPack();
        pack.FrameSize = new AvatarFrameSize { Width = 0, Height = 80 };

        Assert.Contains(AvatarPackValidator.Validate(pack, Sizes()), e => e.Contains("frameSize"));
    }

    [Fact]
    public void MissingIdleAnimation_IsReported()
    {
        var pack = ValidPack();
        pack.Animations.Clear();

        Assert.Contains(AvatarPackValidator.Validate(pack, Sizes()), e => e.Contains("'idle'"));
    }

    [Fact]
    public void ZeroFramesOrFps_AreReported()
    {
        var pack = ValidPack();
        pack.Animations["idle"].Frames = 0;
        pack.Animations["idle"].Fps = 0;

        var errors = AvatarPackValidator.Validate(pack, Sizes());

        Assert.Contains(errors, e => e.Contains("'frames'"));
        Assert.Contains(errors, e => e.Contains("'fps'"));
    }

    [Theory]
    [InlineData("../evil.png")]
    [InlineData("sub/idle.png")]
    [InlineData("sub\\idle.png")]
    public void SheetPaths_AreRejected(string sheet)
    {
        var pack = ValidPack();
        pack.Animations["idle"].Sheet = sheet;

        Assert.Contains(AvatarPackValidator.Validate(pack, Sizes()), e => e.Contains("not a path"));
    }

    [Fact]
    public void MissingSheetFile_IsReported()
    {
        var errors = AvatarPackValidator.Validate(ValidPack(), _ => null);

        Assert.Contains(errors, e => e.Contains("was not found"));
    }

    [Fact]
    public void WrongSheetDimensions_ReportExpectedSize()
    {
        var errors = AvatarPackValidator.Validate(ValidPack(), Sizes(width: 300));

        var error = Assert.Single(errors);
        Assert.Contains("300x80", error);
        Assert.Contains("400x80", error);
    }

    [Fact]
    public void MultipleProblems_AreAllReportedTogether()
    {
        var pack = ValidPack();
        pack.Name = "";
        pack.Animations["wave"] = new AvatarAnimation { Sheet = "wave.png", Frames = 2, Fps = 8 };

        var errors = AvatarPackValidator.Validate(pack, name => name == "idle.png" ? (400, 80) : null);

        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public void KnownAnimations_IncludesTheRequiredOne()
    {
        Assert.Contains(AvatarPackValidator.RequiredAnimation, AvatarPackValidator.KnownAnimations);
    }
}
