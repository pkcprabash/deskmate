using System.IO;
using Deskmate.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Deskmate.Infrastructure.Data;

public class DeskmateDbContext(DbContextOptions<DeskmateDbContext> options) : DbContext(options)
{
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<ReminderOccurrence> ReminderOccurrences => Set<ReminderOccurrence>();

    public static string GetDatabasePath()
    {
        var directory = AppPaths.GetAppDataDirectory();
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "deskmate.db");
    }
}
