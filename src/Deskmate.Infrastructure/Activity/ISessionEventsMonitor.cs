using System;

namespace Deskmate.Infrastructure.Activity;

/// <summary>
/// Fires when the user returns to the machine: the screen unlocks or the
/// machine wakes from sleep. One implementation per OS
/// (<see cref="WindowsSessionEventsMonitor"/>, <see cref="MacSessionEventsMonitor"/>),
/// chosen at startup in AppHost.
/// </summary>
public interface ISessionEventsMonitor
{
    event EventHandler? SessionResumed;
}
