using System;

namespace Deskmate.Core.Models;

/// <summary>One row per actual date a reminder falls on; tracks which alerts have been shown.</summary>
public class ReminderOccurrence
{
    public int Id { get; set; }
    public int ReminderId { get; set; }
    public DateOnly Date { get; set; }
    public bool DayBeforeShown { get; set; }
    public bool OnDayShown { get; set; }
    public bool AdvanceShown { get; set; }
    public bool OverdueShown { get; set; }
    public bool Completed { get; set; }
    public DateTimeOffset? SnoozedUntil { get; set; }
}
