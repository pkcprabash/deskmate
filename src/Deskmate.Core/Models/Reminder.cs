using System;

namespace Deskmate.Core.Models;

public class Reminder
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? Time { get; set; }
    public RecurrenceType Recurrence { get; set; }
    /// <summary>Last date this reminder recurs on, inclusive. Null means it repeats indefinitely.</summary>
    public DateOnly? RecurrenceEndDate { get; set; }
    /// <summary>For Recurrence == Weekly only: specific weekdays it recurs on (e.g. Mon/Wed/Fri).
    /// Null or <see cref="DaysOfWeekFlags.None"/> means the plain case — every week, on Date's own weekday.</summary>
    public DaysOfWeekFlags? RecurrenceWeekdays { get; set; }
    public bool AlertDayBefore { get; set; } = true;
    public bool AlertOnDay { get; set; } = true;
    public TimeSpan? AlertBefore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Manual (the default) or imported from a calendar subscription — see <see cref="ExternalId"/>.</summary>
    public ReminderSource Source { get; set; } = ReminderSource.Manual;

    /// <summary>The calendar event's UID, for reminders with <see cref="Source"/> IcsSubscription. Lets a
    /// later sync recognize "this is the same event" (update it) instead of creating a duplicate.</summary>
    public string? ExternalId { get; set; }
}
