using System.Threading;
using System.Threading.Tasks;
using Deskmate.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Deskmate.Infrastructure.Data;

/// <summary>
/// Reads and writes the single <see cref="UserSettings"/> row, opening a
/// short-lived <see cref="DeskmateDbContext"/> per call rather than holding
/// one open for the app's lifetime.
/// </summary>
public class SettingsService(IDbContextFactory<DeskmateDbContext> dbContextFactory)
{
    public async Task<UserSettings> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var settings = await db.UserSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new UserSettings();
        db.UserSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SavePositionAsync(int settingsId, double positionX, double positionY, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var settings = await db.UserSettings.FindAsync([settingsId], cancellationToken);
        if (settings is null)
        {
            return;
        }

        settings.PositionX = positionX;
        settings.PositionY = positionY;
        await db.SaveChangesAsync(cancellationToken);
    }
}
