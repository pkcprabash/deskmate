using System;
using System.IO;

namespace Deskmate.Infrastructure;

/// <summary>
/// OS-appropriate app-data folder: %LocalAppData%\Deskmate on Windows,
/// ~/Library/Application Support/Deskmate on macOS.
/// </summary>
public static class AppPaths
{
    public static string GetAppDataDirectory() => Path.Combine(GetBaseDataDirectory(), "Deskmate");

    /// <summary>
    /// %LocalAppData% on Windows, ~/Library/Application Support on macOS,
    /// $XDG_DATA_HOME (or ~/.local/share) on Linux.
    /// </summary>
    private static string GetBaseDataDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
        }

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        return string.IsNullOrEmpty(xdgDataHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share")
            : xdgDataHome;
    }
}
