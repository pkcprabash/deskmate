using System;

namespace Deskmate.Core.Models;

/// <summary>One VEVENT read from an ICS calendar feed, reduced to what a Reminder needs.</summary>
public sealed record CalendarEvent(string ExternalId, string Title, DateOnly Date, TimeOnly? Time);
