using System.Collections.Generic;
using System.Linq;
using Deskmate.Core.Models;

namespace Deskmate.Core;

/// <summary>A previously-synced reminder, as far as the planner needs to know about it.</summary>
public sealed record SyncedReminderSnapshot(
    int ReminderId, string ExternalId, string Title, DateOnly Date, TimeOnly? Time,
    RecurrenceType Recurrence, DateOnly? RecurrenceEndDate, DaysOfWeekFlags? RecurrenceWeekdays, int? RecurrenceOrdinal);

public sealed record CalendarSyncPlan(
    IReadOnlyList<CalendarEvent> ToAdd,
    IReadOnlyList<(int ReminderId, CalendarEvent Event)> ToUpdate,
    IReadOnlyList<int> ToRemoveReminderIds);

/// <summary>
/// Diffs a calendar feed's events against the reminders a previous sync created, without
/// touching storage — so the add/update/remove decision is fully unit-testable.
///
/// A previously synced reminder whose event disappeared from the feed is removed only if it's
/// still "live": a one-off event dated on or after <c>windowStart</c> (normally "today"), or a
/// recurring one whose series hasn't already ended before <c>windowStart</c>. A recurring
/// reminder's original start date is judged this way rather than by <c>Date</c> directly,
/// since that date can be years in the past for a series that's still actively recurring.
/// </summary>
public static class CalendarSyncPlanner
{
    public static CalendarSyncPlan Plan(
        IReadOnlyList<SyncedReminderSnapshot> existing, IReadOnlyList<CalendarEvent> incoming, DateOnly windowStart)
    {
        var existingById = existing.ToDictionary(r => r.ExternalId);
        var incomingIds = incoming.Select(e => e.ExternalId).ToHashSet();

        var toAdd = new List<CalendarEvent>();
        var toUpdate = new List<(int, CalendarEvent)>();

        foreach (var calendarEvent in incoming)
        {
            if (!existingById.TryGetValue(calendarEvent.ExternalId, out var reminder))
            {
                toAdd.Add(calendarEvent);
            }
            else if (reminder.Title != calendarEvent.Title || reminder.Date != calendarEvent.Date || reminder.Time != calendarEvent.Time
                || reminder.Recurrence != calendarEvent.Recurrence || reminder.RecurrenceEndDate != calendarEvent.RecurrenceEndDate
                || reminder.RecurrenceWeekdays != calendarEvent.RecurrenceWeekdays || reminder.RecurrenceOrdinal != calendarEvent.RecurrenceOrdinal)
            {
                toUpdate.Add((reminder.ReminderId, calendarEvent));
            }
        }

        var toRemove = existing
            .Where(r => !incomingIds.Contains(r.ExternalId) && IsStillLive(r, windowStart))
            .Select(r => r.ReminderId)
            .ToList();

        return new CalendarSyncPlan(toAdd, toUpdate, toRemove);
    }

    private static bool IsStillLive(SyncedReminderSnapshot reminder, DateOnly windowStart) =>
        reminder.Recurrence == RecurrenceType.None
            ? reminder.Date >= windowStart
            : reminder.RecurrenceEndDate is not { } end || end >= windowStart;
}
