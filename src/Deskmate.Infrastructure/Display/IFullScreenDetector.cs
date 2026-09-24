namespace Deskmate.Infrastructure.Display;

/// <summary>
/// Detects whether some other app is currently full-screen (or presenting), so the avatar
/// can hide itself rather than float on top of a video call or a slide deck. One
/// implementation per OS (<see cref="WindowsFullScreenDetector"/>, <see cref="MacFullScreenDetector"/>),
/// chosen at startup in AppHost.
/// </summary>
public interface IFullScreenDetector
{
    bool IsFullScreenAppActive();
}
