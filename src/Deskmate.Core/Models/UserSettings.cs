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
    public bool StartAtLogin { get; set; } = true;
    public DateOnly? LastGreetingDate { get; set; }
    public bool FirstRunCompleted { get; set; }
}
