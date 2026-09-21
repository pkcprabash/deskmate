using System;

namespace Deskmate.Infrastructure.Activity;

/// <summary>
/// Derives idle duration from the keyboard monitor's last-seen-event
/// timestamp — no separate platform idle API needed.
/// </summary>
public class IdleMonitor(KeyboardActivityMonitor keyboardActivityMonitor)
{
    public TimeSpan GetIdleDuration() => DateTimeOffset.UtcNow - keyboardActivityMonitor.LastActivityAtUtc;

    public bool IsIdleFor(TimeSpan threshold) => GetIdleDuration() >= threshold;
}
