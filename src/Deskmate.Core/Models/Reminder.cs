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
    public bool AlertDayBefore { get; set; } = true;
    public bool AlertOnDay { get; set; } = true;
    public TimeSpan? AlertBefore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
