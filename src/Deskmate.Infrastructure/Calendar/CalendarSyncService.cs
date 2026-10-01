using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Deskmate.Core;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Deskmate.Infrastructure.Calendar;

/// <summary>
/// Fetches a calendar's private ICS subscription URL — the "secret address" Google, Outlook,
/// and Apple Calendar each let you copy out, with no OAuth app registration needed — and syncs
/// it into reminders. One HttpClient is shared for the app's lifetime, per Microsoft's guidance
/// against socket exhaustion from creating one per call.
/// </summary>
public sealed class CalendarSyncService(ReminderService reminderService, ILogger<CalendarSyncService> logger) : IDisposable
{
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(20);

    /// <summary>How far ahead events are imported. Far enough for day-before/on-day alerts to matter, not so far that a huge feed is fetched in full each time.</summary>
    private static readonly int SyncWindowDays = 180;

    private readonly HttpClient _httpClient = new() { Timeout = HttpTimeout };

    /// <returns>Null on success (with the result), or an error message safe to show in Settings.</returns>
    public async Task<(CalendarSyncResult? Result, string? Error)> SyncAsync(string icsUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(icsUrl))
        {
            return (null, "No calendar URL is set.");
        }

        if (!Uri.TryCreate(icsUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https" or "webcal"))
        {
            return (null, "That doesn't look like a valid calendar URL.");
        }

        // "webcal://" is the same content over HTTP(S); browsers/calendar apps use it only to
        // hint "open in a calendar app", which doesn't apply here.
        if (uri.Scheme == "webcal")
        {
            uri = new UriBuilder(uri) { Scheme = "https" }.Uri;
        }

        try
        {
            var icsText = await _httpClient.GetStringAsync(uri, cancellationToken);
            var events = IcsParser.Parse(icsText);

            var today = DateOnly.FromDateTime(DateTime.Now);
            var withinWindow = events.Where(e => e.Date >= today && e.Date <= today.AddDays(SyncWindowDays)).ToList();

            var result = await reminderService.ApplyCalendarSyncAsync(withinWindow, today, cancellationToken);
            return (result, null);
        }
        catch (OperationCanceledException)
        {
            return (null, "The calendar didn't respond in time.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Calendar sync failed to fetch the feed.");
            return (null, "Couldn't reach that calendar URL.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Calendar sync failed.");
            return (null, "Something went wrong reading that calendar.");
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
