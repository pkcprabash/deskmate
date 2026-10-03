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

    /// <summary>
    /// The date of the Nth (1-4) or last-counting-back (-1 to -4) <paramref name="weekday"/> in
    /// <paramref name="year"/>/<paramref name="month"/> — "the 3rd Thursday" or "the last Friday"
    /// of that month. Null if that ordinal doesn't exist in the month (e.g. a "5th" weekday when
    /// the month only has four).
    /// </summary>
    public static DateOnly? NthWeekdayOfMonth(int year, int month, DayOfWeek weekday, int ordinal)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);

        if (ordinal > 0)
        {
            var firstOfMonth = new DateOnly(year, month, 1);
            var firstMatch = 1 + ((int)weekday - (int)firstOfMonth.DayOfWeek + 7) % 7;
            var day = firstMatch + (ordinal - 1) * 7;
            return day <= daysInMonth ? new DateOnly(year, month, day) : null;
        }

        if (ordinal < 0)
        {
            var lastOfMonth = new DateOnly(year, month, daysInMonth);
            var lastMatch = daysInMonth - (((int)lastOfMonth.DayOfWeek - (int)weekday + 7) % 7);
            var day = lastMatch + (ordinal + 1) * 7; // ordinal is negative, so this steps backward
            return day >= 1 ? new DateOnly(year, month, day) : null;
        }

        return null; // ordinal 0 isn't a valid "Nth" or "last" — not RFC 5545's BYDAY syntax.
    }
}
