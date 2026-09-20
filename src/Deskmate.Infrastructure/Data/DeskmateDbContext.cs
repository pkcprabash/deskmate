using System;
using System.IO;
using Deskmate.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Deskmate.Infrastructure.Data;

public class DeskmateDbContext(DbContextOptions<DeskmateDbContext> options) : DbContext(options)
{
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    /// <summary>
    /// OS-appropriate app-data folder: %LocalAppData%\Deskmate on Windows,
    /// ~/Library/Application Support/Deskmate on macOS.
    /// </summary>
    public static string GetAppDataDirectory()
    {
        var baseDirectory = OperatingSystem.IsWindows()
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");

        return Path.Combine(baseDirectory, "Deskmate");
    }

    public static string GetDatabasePath()
    {
        var directory = GetAppDataDirectory();
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "deskmate.db");
    }
}
