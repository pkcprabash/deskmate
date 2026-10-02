using System;
using Deskmate.Core.Models;

namespace Deskmate.Core;

/// <summary>
/// The date-stepping rule shared by <see cref="ReminderRules"/> (which occurrence dates a
/// recurring reminder falls on) and <see cref="IcsParser"/> (computing the end date of a
/// calendar event's RRULE COUNT, so it lands on the same dates a step-by-step walk would).
/// </summary>
internal static class RecurrenceStep
{
    /// <summary>The date of the Nth occurrence (0 = <paramref name="start"/> itself) at interval 1.</summary>
    public static DateOnly NthOccurrence(DateOnly start, RecurrenceType recurrence, int occurrenceIndex) => recurrence switch
    {
        RecurrenceType.Daily => start.AddDays(occurrenceIndex),
        RecurrenceType.Weekly => start.AddDays(occurrenceIndex * 7),
        RecurrenceType.Monthly => AddMonthsClamped(start, occurrenceIndex),
        RecurrenceType.Yearly => AddYearsClamped(start, occurrenceIndex),
        _ => start,
    };

    private static DateOnly AddMonthsClamped(DateOnly date, int months)
    {
        var totalMonths = date.Month - 1 + months;
        var year = date.Year + totalMonths / 12;
        var month = totalMonths % 12 + 1;
        var day = Math.Min(date.Day, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, day);
    }

    private static DateOnly AddYearsClamped(DateOnly date, int years)
    {
        var year = date.Year + years;
        var day = Math.Min(date.Day, DateTime.DaysInMonth(year, date.Month));
        return new DateOnly(year, date.Month, day);
    }
}
