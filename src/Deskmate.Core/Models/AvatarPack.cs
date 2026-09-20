using System.Collections.Generic;

namespace Deskmate.Core.Models;

public class AvatarPack
{
    public string Name { get; set; } = "";
    public AvatarFrameSize FrameSize { get; set; } = new();
    public Dictionary<string, AvatarAnimation> Animations { get; set; } = new();
}

public class AvatarFrameSize
{
    public int Width { get; set; }
    public int Height { get; set; }
}

public class AvatarAnimation
{
    public string Sheet { get; set; } = "";
    public int Frames { get; set; }
    public int Fps { get; set; }
    public bool Loop { get; set; }
}
