using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deskmate.Core;
using Deskmate.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Deskmate.Infrastructure.Data;

/// <summary>
/// CRUD for <see cref="Reminder"/> plus the <see cref="ReminderOccurrence"/> bookkeeping
/// that <see cref="ReminderRules"/> needs. Opens a short-lived <see cref="DeskmateDbContext"/>
/// per call rather than holding one open for the app's lifetime.
/// </summary>
public class ReminderService(IDbContextFactory<DeskmateDbContext> dbContextFactory)
{
    public async Task<List<Reminder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Reminders.OrderBy(r => r.Date).ToListAsync(cancellationToken);
    }

    public async Task<Reminder> AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);
        return reminder;
    }

    public async Task UpdateAsync(int reminderId, Action<Reminder> apply, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reminder = await db.Reminders.FindAsync([reminderId], cancellationToken);
        if (reminder is null)
        {
            return;
        }

        apply(reminder);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int reminderId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await db.ReminderOccurrences.Where(o => o.ReminderId == reminderId).ExecuteDeleteAsync(cancellationToken);
        await db.Reminders.Where(r => r.Id == reminderId).ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>Marks the earliest not-yet-completed occurrence of this reminder as done.</summary>
    public async Task MarkNextOccurrenceCompletedAsync(int reminderId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var occurrence = await db.ReminderOccurrences
            .Where(o => o.ReminderId == reminderId && !o.Completed)
            .OrderBy(o => o.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (occurrence is null)
        {
            return;
        }

        occurrence.Completed = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Generates any missing occurrences, from today through <paramref name="through"/>, for every reminder.</summary>
    public async Task EnsureOccurrencesGeneratedAsync(ReminderRules rules, DateOnly today, DateOnly through, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var reminders = await db.Reminders.ToListAsync(cancellationToken);

        foreach (var reminder in reminders)
        {
            var existingDates = await db.ReminderOccurrences
                .Where(o => o.ReminderId == reminder.Id)
                .Select(o => o.Date)
                .ToListAsync(cancellationToken);
            var existingDateSet = existingDates.ToHashSet();

            foreach (var date in rules.GenerateOccurrenceDates(reminder, today, through))
            {
                if (!existingDateSet.Contains(date))
                {
                    db.ReminderOccurrences.Add(new ReminderOccurrence { ReminderId = reminder.Id, Date = date });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>All not-yet-completed occurrences, paired with their reminder, for the scheduler to evaluate.</summary>
    public async Task<List<(Reminder Reminder, ReminderOccurrence Occurrence)>> GetPendingOccurrencesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var pending = await db.ReminderOccurrences
            .Where(o => !o.Completed)
            .Join(db.Reminders, o => o.ReminderId, r => r.Id, (o, r) => new { Occurrence = o, Reminder = r })
            .ToListAsync(cancellationToken);

        return pending.Select(p => (p.Reminder, p.Occurrence)).ToList();
    }

    public async Task MarkOccurrenceShownAsync(int occurrenceId, ReminderRules rules, ReminderAlertKind kind, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var occurrence = await db.ReminderOccurrences.FindAsync([occurrenceId], cancellationToken);
        if (occurrence is null)
        {
            return;
        }

        rules.MarkShown(occurrence, kind);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkOccurrenceCompletedAsync(int occurrenceId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var occurrence = await db.ReminderOccurrences.FindAsync([occurrenceId], cancellationToken);
        if (occurrence is null)
        {
            return;
        }

        occurrence.Completed = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SnoozeOccurrenceAsync(int occurrenceId, DateTimeOffset until, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var occurrence = await db.ReminderOccurrences.FindAsync([occurrenceId], cancellationToken);
        if (occurrence is null)
        {
            return;
        }

        occurrence.SnoozedUntil = until;
        await db.SaveChangesAsync(cancellationToken);
    }
}
