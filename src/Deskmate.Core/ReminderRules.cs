using System;
using System.Collections.Generic;
using Deskmate.Core.Models;

namespace Deskmate.Core;

/// <summary>
/// Recurrence expansion and alert timing for reminders. No UI or storage
/// dependency, so it's fully unit-testable.
/// </summary>
public class ReminderRules
{
    /// <summary>Dates this reminder falls on within [from, through], inclusive.</summary>
    public IEnumerable<DateOnly> GenerateOccurrenceDates(Reminder reminder, DateOnly from, DateOnly through)
    {
        if (reminder.Recurrence == RecurrenceType.None)
        {
            if (reminder.Date >= from && reminder.Date <= through)
            {
                yield return reminder.Date;
            }

            yield break;
        }

        var occurrenceIndex = 0;
        var current = reminder.Date;
        var lastDate = reminder.RecurrenceEndDate is { } end && end < through ? end : through;

        while (current <= lastDate)
        {
            if (current >= from)
            {
                yield return current;
            }

            occurrenceIndex++;
            current = reminder.Recurrence == RecurrenceType.None
                ? through.AddDays(1) // unreachable in practice (None returns early above); guards the loop if it ever isn't
                : RecurrenceStep.NthOccurrence(reminder.Date, reminder.Recurrence, occurrenceIndex);
        }
    }

    /// <summary>
    /// Which alert (if any) is due right now for this occurrence. Callers check this
    /// once per pending occurrence on each scheduler tick, then call <see cref="MarkShown"/>
    /// once they've actually shown it.
    /// </summary>
    public ReminderAlertKind DetermineAlertKind(Reminder reminder, ReminderOccurrence occurrence, DateTimeOffset now)
    {
        if (occurrence.Completed)
        {
            return ReminderAlertKind.None;
        }

        if (occurrence.SnoozedUntil is { } snoozedUntil && now < snoozedUntil)
        {
            return ReminderAlertKind.None;
        }

        // now.Date (not now.LocalDateTime): callers pass a DateTimeOffset whose own
        // offset already represents the user's local time (as DateTimeOffset.Now does),
        // so re-converting via LocalDateTime would wrongly apply the machine's system
        // timezone on top of that.
        var today = DateOnly.FromDateTime(now.Date);

        if (!occurrence.AdvanceShown && reminder.AlertBefore is { } advance && reminder.Time is { } time && occurrence.Date == today)
        {
            var dueAt = new DateTimeOffset(occurrence.Date.ToDateTime(time), now.Offset) - advance;
            if (now >= dueAt)
            {
                return ReminderAlertKind.Advance;
            }
        }

        if (!occurrence.DayBeforeShown && reminder.AlertDayBefore && today == occurrence.Date.AddDays(-1))
        {
            return ReminderAlertKind.DayBefore;
        }

        if (!occurrence.OnDayShown && reminder.AlertOnDay && today == occurrence.Date)
        {
            if (reminder.Time is null || now.TimeOfDay >= reminder.Time.Value.ToTimeSpan())
            {
                return ReminderAlertKind.OnDay;
            }
        }

        if (!occurrence.OverdueShown && occurrence.OnDayShown && today > occurrence.Date)
        {
            return ReminderAlertKind.Overdue;
        }

        return ReminderAlertKind.None;
    }

    public void MarkShown(ReminderOccurrence occurrence, ReminderAlertKind kind)
    {
        switch (kind)
        {
            case ReminderAlertKind.Advance:
                occurrence.AdvanceShown = true;
                break;
            case ReminderAlertKind.DayBefore:
                occurrence.DayBeforeShown = true;
                break;
            case ReminderAlertKind.OnDay:
                occurrence.OnDayShown = true;
                break;
            case ReminderAlertKind.Overdue:
                occurrence.OverdueShown = true;
                break;
        }
    }

}
