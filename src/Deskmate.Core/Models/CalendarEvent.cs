using System;

namespace Deskmate.Core.Models;

/// <summary>
/// One VEVENT read from an ICS calendar feed, reduced to what a Reminder needs.
/// <see cref="Recurrence"/> is set only for the simple RRULE shapes <see cref="IcsParser"/>
/// understands (see its docs); anything more complex is treated as a one-off event, or the
/// whole VEVENT is skipped if it can't be trusted at all.
/// </summary>
public sealed record CalendarEvent(
    string ExternalId,
    string Title,
    DateOnly Date,
    TimeOnly? Time,
    RecurrenceType Recurrence = RecurrenceType.None,
    DateOnly? RecurrenceEndDate = null,
    DaysOfWeekFlags? RecurrenceWeekdays = null,
    int? RecurrenceOrdinal = null);
