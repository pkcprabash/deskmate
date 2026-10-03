using System;

namespace Deskmate.Core.Models;

/// <summary>A set of weekdays, for a reminder that recurs on specific days (e.g. "every Mon/Wed/Fri").</summary>
[Flags]
public enum DaysOfWeekFlags
{
    None = 0,
    Monday = 1 << 0,
    Tuesday = 1 << 1,
    Wednesday = 1 << 2,
    Thursday = 1 << 3,
    Friday = 1 << 4,
    Saturday = 1 << 5,
    Sunday = 1 << 6,
}

public static class DaysOfWeekFlagsExtensions
{
    public static DaysOfWeekFlags ToFlag(this DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => DaysOfWeekFlags.Monday,
        DayOfWeek.Tuesday => DaysOfWeekFlags.Tuesday,
        DayOfWeek.Wednesday => DaysOfWeekFlags.Wednesday,
        DayOfWeek.Thursday => DaysOfWeekFlags.Thursday,
        DayOfWeek.Friday => DaysOfWeekFlags.Friday,
        DayOfWeek.Saturday => DaysOfWeekFlags.Saturday,
        DayOfWeek.Sunday => DaysOfWeekFlags.Sunday,
        _ => DaysOfWeekFlags.None,
    };

    public static bool Contains(this DaysOfWeekFlags flags, DayOfWeek day) => (flags & day.ToFlag()) != 0;

    /// <summary>The single weekday a one-bit set represents, for the "ordinal weekday" recurrence
    /// case (e.g. "the 3rd Thursday"), where exactly one weekday is meaningful. Null if
    /// <paramref name="flags"/> is empty or has more than one bit set.</summary>
    public static DayOfWeek? ToSingleDayOfWeek(this DaysOfWeekFlags flags) => flags switch
    {
        DaysOfWeekFlags.Monday => DayOfWeek.Monday,
        DaysOfWeekFlags.Tuesday => DayOfWeek.Tuesday,
        DaysOfWeekFlags.Wednesday => DayOfWeek.Wednesday,
        DaysOfWeekFlags.Thursday => DayOfWeek.Thursday,
        DaysOfWeekFlags.Friday => DayOfWeek.Friday,
        DaysOfWeekFlags.Saturday => DayOfWeek.Saturday,
        DaysOfWeekFlags.Sunday => DayOfWeek.Sunday,
        _ => null, // None, or more than one bit set
    };
}
