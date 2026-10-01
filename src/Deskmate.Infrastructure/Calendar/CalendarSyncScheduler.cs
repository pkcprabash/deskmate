using System;
using System.Threading;
using System.Threading.Tasks;
using Deskmate.Infrastructure.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Deskmate.Infrastructure.Calendar;

/// <summary>Re-syncs the subscribed calendar (if one is configured) once at startup and then periodically.</summary>
public sealed class CalendarSyncScheduler(
    CalendarSyncService calendarSyncService, SettingsService settingsService, ILogger<CalendarSyncScheduler> logger)
    : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(4);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncIfConfiguredAsync(stoppingToken);

            try
            {
                await Task.Delay(SyncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncIfConfiguredAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await settingsService.GetOrCreateAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(settings.CalendarIcsUrl))
            {
                return;
            }

            var (result, error) = await calendarSyncService.SyncAsync(settings.CalendarIcsUrl, cancellationToken);
            await settingsService.UpdateAsync(settings.Id, s =>
            {
                s.LastCalendarSyncAt = DateTimeOffset.Now;
                s.LastCalendarSyncError = error;
            }, cancellationToken);

            if (result is { } r)
            {
                logger.LogInformation("Calendar sync: {Added} added, {Updated} updated, {Removed} removed.", r.Added, r.Updated, r.Removed);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Background calendar sync failed.");
        }
    }
}
