using System;
using Deskmate.Core;

namespace Deskmate.Core.Tests;

public class PomodoroTimerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 25, 9, 0, 0, TimeSpan.Zero);

    private static PomodoroTimer CreateTimer(int sessionsBeforeLongBreak = 2) => new()
    {
        Settings = new PomodoroSettings(
            TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), sessionsBeforeLongBreak),
    };

    [Fact]
    public void NewTimer_IsStopped()
    {
        var timer = CreateTimer();

        Assert.Equal(PomodoroPhase.Stopped, timer.Phase);
        Assert.False(timer.IsRunning);
        Assert.False(timer.Tick(T0.AddHours(5)));
        Assert.Equal(TimeSpan.Zero, timer.Remaining(T0));
    }

    [Fact]
    public void Start_BeginsFocusWithFullDuration()
    {
        var timer = CreateTimer();

        timer.Start(T0);

        Assert.Equal(PomodoroPhase.Focus, timer.Phase);
        Assert.Equal(TimeSpan.FromMinutes(25), timer.Remaining(T0));
        Assert.Equal(TimeSpan.FromMinutes(20), timer.Remaining(T0.AddMinutes(5)));
    }

    [Fact]
    public void Tick_BeforeEnd_DoesNotChangePhase()
    {
        var timer = CreateTimer();
        timer.Start(T0);

        Assert.False(timer.Tick(T0.AddMinutes(24)));
        Assert.Equal(PomodoroPhase.Focus, timer.Phase);
    }

    [Fact]
    public void Tick_AtEndOfFocus_StartsShortBreak()
    {
        var timer = CreateTimer();
        timer.Start(T0);

        Assert.True(timer.Tick(T0.AddMinutes(25)));

        Assert.Equal(PomodoroPhase.ShortBreak, timer.Phase);
        Assert.Equal(1, timer.CompletedFocusSessions);
        Assert.Equal(TimeSpan.FromMinutes(5), timer.Remaining(T0.AddMinutes(25)));
    }

    [Fact]
    public void Tick_AfterBreak_ReturnsToFocus()
    {
        var timer = CreateTimer();
        timer.Start(T0);
        timer.Tick(T0.AddMinutes(25));

        Assert.True(timer.Tick(T0.AddMinutes(30)));

        Assert.Equal(PomodoroPhase.Focus, timer.Phase);
    }

    [Fact]
    public void Tick_EveryNthFocus_StartsLongBreak()
    {
        var timer = CreateTimer(sessionsBeforeLongBreak: 2);
        timer.Start(T0);
        timer.Tick(T0.AddMinutes(25)); // short break
        timer.Tick(T0.AddMinutes(30)); // focus #2
        timer.Tick(T0.AddMinutes(55)); // end of focus #2

        Assert.Equal(PomodoroPhase.LongBreak, timer.Phase);
        Assert.Equal(TimeSpan.FromMinutes(15), timer.Remaining(T0.AddMinutes(55)));
    }

    [Fact]
    public void Tick_AfterLongGap_AdvancesOnlyOnePhase()
    {
        var timer = CreateTimer();
        timer.Start(T0);
        var wake = T0.AddHours(8); // laptop slept through the whole session

        Assert.True(timer.Tick(wake));

        Assert.Equal(PomodoroPhase.ShortBreak, timer.Phase);
        Assert.False(timer.Tick(wake.AddSeconds(1)));
        Assert.Equal(TimeSpan.FromMinutes(5), timer.Remaining(wake));
    }

    [Fact]
    public void Skip_EndsCurrentPhaseEarly()
    {
        var timer = CreateTimer();
        timer.Start(T0);

        Assert.True(timer.Skip(T0.AddMinutes(3)));

        Assert.Equal(PomodoroPhase.ShortBreak, timer.Phase);
        Assert.Equal(1, timer.CompletedFocusSessions);
    }

    [Fact]
    public void Skip_WhenStopped_DoesNothing()
    {
        var timer = CreateTimer();

        Assert.False(timer.Skip(T0));
        Assert.Equal(PomodoroPhase.Stopped, timer.Phase);
    }

    [Fact]
    public void Stop_ResetsPhaseAndCount()
    {
        var timer = CreateTimer();
        timer.Start(T0);
        timer.Tick(T0.AddMinutes(25));

        timer.Stop();

        Assert.Equal(PomodoroPhase.Stopped, timer.Phase);
        Assert.Equal(0, timer.CompletedFocusSessions);
    }

    [Fact]
    public void Start_AfterStop_BeginsFreshCycle()
    {
        var timer = CreateTimer(sessionsBeforeLongBreak: 2);
        timer.Start(T0);
        timer.Tick(T0.AddMinutes(25));
        timer.Stop();

        timer.Start(T0.AddHours(1));
        timer.Tick(T0.AddHours(1).AddMinutes(25));

        Assert.Equal(PomodoroPhase.ShortBreak, timer.Phase); // count restarted, so not a long break yet
    }

    [Fact]
    public void ZeroSessionsBeforeLongBreak_IsTreatedAsOne()
    {
        var timer = CreateTimer(sessionsBeforeLongBreak: 0);
        timer.Start(T0);

        timer.Tick(T0.AddMinutes(25));

        Assert.Equal(PomodoroPhase.LongBreak, timer.Phase);
    }
}
