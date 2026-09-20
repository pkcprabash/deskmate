using System;
using System.IO;

namespace Deskmate.Infrastructure;

/// <summary>
/// OS-appropriate app-data folder: %LocalAppData%\Deskmate on Windows,
/// ~/Library/Application Support/Deskmate on macOS.
/// </summary>
public static class AppPaths
{
    public static string GetAppDataDirectory()
    {
        var baseDirectory = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");

        return Path.Combine(baseDirectory, "Deskmate");
    }
}
