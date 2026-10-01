using System;

namespace Deskmate.Core.Models;

public class UserSettings
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string AvatarName { get; set; } = "Pixel";
    public string AvatarPack { get; set; } = "mint";
    public double AvatarScale { get; set; } = 1.0;
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public TimeOnly WorkStart { get; set; } = new(9, 0);
    public TimeOnly WorkEnd { get; set; } = new(17, 0);
    public string CurrentFocus { get; set; } = "";
    public MessageTone Tone { get; set; } = MessageTone.Cheerful;
    public TimeSpan SleepAfter { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan BreakAfter { get; set; } = TimeSpan.FromMinutes(50);
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }
    public bool ReducedMotion { get; set; }
    public TimeSpan PomodoroFocus { get; set; } = TimeSpan.FromMinutes(25);
    public TimeSpan PomodoroShortBreak { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan PomodoroLongBreak { get; set; } = TimeSpan.FromMinutes(15);
    public int PomodoroSessionsBeforeLongBreak { get; set; } = 4;
    public bool AiMessagesEnabled { get; set; }
    /// <summary>Your own Anthropic API key. Stored locally only, in this app's SQLite database; never sent anywhere but api.anthropic.com.</summary>
    public string AiApiKey { get; set; } = "";
    public string AiModel { get; set; } = "claude-haiku-4-5";
    /// <summary>A calendar's private "secret address" / iCal subscription URL (Google, Outlook, Apple all offer one). Empty disables sync.</summary>
    public string CalendarIcsUrl { get; set; } = "";
    public DateTimeOffset? LastCalendarSyncAt { get; set; }
    public string? LastCalendarSyncError { get; set; }
    public bool StartAtLogin { get; set; } = true;
    public DateOnly? LastGreetingDate { get; set; }
    public bool FirstRunCompleted { get; set; }
}
