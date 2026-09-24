using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Deskmate.Core;
using Deskmate.Core.Models;
using Deskmate.Infrastructure.Activity;
using Deskmate.Infrastructure.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Deskmate.Infrastructure.Reminders;

/// <summary>
/// Checks once a minute (and immediately on session resume, to catch up after the
/// laptop was asleep) for any reminder occurrence that's due an alert. Generates
/// occurrences a few weeks ahead so recurring reminders never run dry.
/// </summary>
public sealed class ReminderScheduler(
    ReminderService reminderService,
    SettingsService settingsService,
    ISessionEventsMonitor sessionEventsMonitor,
    ILogger<ReminderScheduler> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private static readonly int GenerateAheadDays = 21;

    private readonly ReminderRules _rules = new();
    private readonly List<(Reminder Reminder, ReminderOccurrence Occurrence, ReminderAlertKind Kind)> _undelivered = [];
    private readonly Lock _undeliveredLock = new();

    public event Action<Reminder, ReminderOccurrence, ReminderAlertKind>? AlertDue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // This hosted service starts (via host.Start() in Program.cs) before the avatar
        // window exists, so its very first check can easily run before anyone has
        // subscribed to AlertDue. ReplayUndelivered() lets that subscriber catch up.
        sessionEventsMonitor.SessionResumed += async (_, _) => await CheckAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAsync(stoppingToken);

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Delivers any alerts that fired before a subscriber was attached. Call once, right after subscribing.</summary>
    public void ReplayUndelivered()
    {
        List<(Reminder, ReminderOccurrence, ReminderAlertKind)> toDeliver;
        lock (_undeliveredLock)
        {
            toDeliver = [.. _undelivered];
            _undelivered.Clear();
        }

        foreach (var (reminder, occurrence, kind) in toDeliver)
        {
            AlertDue?.Invoke(reminder, occurrence, kind);
        }
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.Now;
            var today = DateOnly.FromDateTime(now.Date);
            await reminderService.EnsureOccurrencesGeneratedAsync(_rules, today, today.AddDays(GenerateAheadDays), cancellationToken);

            var settings = await settingsService.GetOrCreateAsync(cancellationToken);
            var withinQuietHours = QuietHours.IsWithin(settings.QuietHoursStart, settings.QuietHoursEnd, TimeOnly.FromDateTime(now.DateTime));
            if (withinQuietHours)
            {
                // Leave occurrences unmarked so the next check (once quiet hours end) picks them up fresh.
                return;
            }

            var pending = await reminderService.GetPendingOccurrencesAsync(cancellationToken);
            foreach (var (reminder, occurrence) in pending)
            {
                var kind = _rules.DetermineAlertKind(reminder, occurrence, now);
                if (kind == ReminderAlertKind.None)
                {
                    continue;
                }

                await reminderService.MarkOccurrenceShownAsync(occurrence.Id, _rules, kind, cancellationToken);
                Deliver(reminder, occurrence, kind);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Reminder check failed.");
        }
    }

    private void Deliver(Reminder reminder, ReminderOccurrence occurrence, ReminderAlertKind kind)
    {
        if (AlertDue is null)
        {
            lock (_undeliveredLock)
            {
                _undelivered.Add((reminder, occurrence, kind));
            }

            return;
        }

        AlertDue.Invoke(reminder, occurrence, kind);
    }
}
