using System;
using System.IO;
using System.Threading.Tasks;

namespace Deskmate.Infrastructure.Startup;

/// <summary>Linux implementation: an XDG autostart .desktop file under ~/.config/autostart.</summary>
public sealed class LinuxStartupRegistration : IStartupRegistration
{
    private const string DesktopFileName = "deskmate.desktop";

    private static string DesktopFilePath => Path.Combine(GetAutostartDirectory(), DesktopFileName);

    private static string GetAutostartDirectory()
    {
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var configHome = string.IsNullOrEmpty(xdgConfigHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : xdgConfigHome;

        return Path.Combine(configHome, "autostart");
    }

    public Task EnableAsync()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            return Task.CompletedTask;
        }

        var desktopEntry = $"""
            [Desktop Entry]
            Type=Application
            Name=Deskmate
            Comment=A small animated coworker in the corner of your screen
            Exec="{executablePath}" --startup
            X-GNOME-Autostart-enabled=true
            NoDisplay=false
            Terminal=false
            """;

        Directory.CreateDirectory(GetAutostartDirectory());
        File.WriteAllText(DesktopFilePath, desktopEntry + Environment.NewLine);
        return Task.CompletedTask;
    }

    public Task DisableAsync()
    {
        if (File.Exists(DesktopFilePath))
        {
            File.Delete(DesktopFilePath);
        }

        return Task.CompletedTask;
    }
}
