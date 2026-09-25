using System;

namespace Deskmate.Core;

public enum PomodoroPhase
{
    Stopped,
    Focus,
    ShortBreak,
    LongBreak,
}

/// <summary>Durations for a Pomodoro cycle.</summary>
public sealed record PomodoroSettings(
    TimeSpan FocusDuration,
    TimeSpan ShortBreakDuration,
    TimeSpan LongBreakDuration,
    int FocusSessionsBeforeLongBreak)
{
    public static PomodoroSettings Default { get; } = new(
        TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), 4);
}

/// <summary>
/// Focus/break cycle with no UI or clock dependency: the caller passes "now" in, so it can be
/// fully unit-tested. Once started it keeps cycling (focus, short break, focus, ... long break)
/// until stopped. If the caller stops ticking for a long time (e.g. the laptop slept), a single
/// <see cref="Tick"/> advances at most one phase and restarts the timer from "now" instead of
/// racing through every phase that was missed.
/// </summary>
public class PomodoroTimer
{
    private DateTimeOffset _phaseEndsAt;

    public PomodoroSettings Settings { get; set; } = PomodoroSettings.Default;

    public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Stopped;

    public bool IsRunning => Phase != PomodoroPhase.Stopped;

    /// <summary>Focus sessions finished since the timer was started.</summary>
    public int CompletedFocusSessions { get; private set; }

    public void Start(DateTimeOffset now)
    {
        CompletedFocusSessions = 0;
        EnterPhase(PomodoroPhase.Focus, now);
    }

    public void Stop()
    {
        Phase = PomodoroPhase.Stopped;
        CompletedFocusSessions = 0;
    }

    /// <summary>Time left in the current phase; zero when stopped.</summary>
    public TimeSpan Remaining(DateTimeOffset now) =>
        IsRunning ? TimeSpan.FromTicks(Math.Max(0, (_phaseEndsAt - now).Ticks)) : TimeSpan.Zero;

    /// <summary>Moves to the next phase (ending the current one early). Returns false if stopped.</summary>
    public bool Skip(DateTimeOffset now)
    {
        if (!IsRunning)
        {
            return false;
        }

        AdvancePhase(now);
        return true;
    }

    /// <summary>Advances the phase if the current one has run out. Returns true if the phase changed.</summary>
    public bool Tick(DateTimeOffset now)
    {
        if (!IsRunning || now < _phaseEndsAt)
        {
            return false;
        }

        AdvancePhase(now);
        return true;
    }

    private void AdvancePhase(DateTimeOffset now)
    {
        if (Phase == PomodoroPhase.Focus)
        {
            CompletedFocusSessions++;
            var longBreakDue = CompletedFocusSessions % Math.Max(1, Settings.FocusSessionsBeforeLongBreak) == 0;
            EnterPhase(longBreakDue ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak, now);
        }
        else
        {
            EnterPhase(PomodoroPhase.Focus, now);
        }
    }

    private void EnterPhase(PomodoroPhase phase, DateTimeOffset now)
    {
        Phase = phase;
        _phaseEndsAt = now + phase switch
        {
            PomodoroPhase.Focus => Settings.FocusDuration,
            PomodoroPhase.ShortBreak => Settings.ShortBreakDuration,
            _ => Settings.LongBreakDuration,
        };
    }
}
