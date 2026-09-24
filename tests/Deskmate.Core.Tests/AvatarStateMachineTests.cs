using System;
using Deskmate.Core;

namespace Deskmate.Core.Tests;

public class AvatarStateMachineTests
{
    [Fact]
    public void Clicked_FromIdle_TransitionsToWaving()
    {
        var sut = new AvatarStateMachine();

        sut.Apply(AvatarInput.Clicked);

        Assert.Equal(AvatarState.Waving, sut.CurrentState);
    }

    [Fact]
    public void OneShotState_ReturnsToIdle_WhenAnimationCompletes()
    {
        var sut = new AvatarStateMachine();
        sut.Apply(AvatarInput.Clicked); // Idle -> Waving, a one-shot state.

        sut.Apply(AvatarInput.AnimationCompleted);

        Assert.Equal(AvatarState.Idle, sut.CurrentState);
    }

    [Fact]
    public void NonOneShotState_StaysPut_WhenAnimationCompletes()
    {
        var sut = new AvatarStateMachine();
        sut.Apply(AvatarInput.DragStarted); // Idle -> Held, not a one-shot state.

        sut.Apply(AvatarInput.AnimationCompleted);

        Assert.Equal(AvatarState.Held, sut.CurrentState);
    }

    [Fact]
    public void DragStarted_TransitionsToHeld_AndDragEnded_ReturnsToIdle()
    {
        var sut = new AvatarStateMachine();

        sut.Apply(AvatarInput.DragStarted);
        Assert.Equal(AvatarState.Held, sut.CurrentState);

        sut.Apply(AvatarInput.DragEnded);
        Assert.Equal(AvatarState.Idle, sut.CurrentState);
    }

    [Fact]
    public void KeyPressed_FromIdle_TransitionsToTyping()
    {
        var sut = new AvatarStateMachine();

        sut.Apply(AvatarInput.KeyPressed);

        Assert.Equal(AvatarState.Typing, sut.CurrentState);
    }

    [Fact]
    public void Tick_LongIdle_FallsAsleep()
    {
        var sut = new AvatarStateMachine { SleepAfter = TimeSpan.FromMinutes(10) };

        sut.Tick(idleFor: TimeSpan.FromMinutes(10), activeFor: TimeSpan.Zero);

        Assert.Equal(AvatarState.Sleeping, sut.CurrentState);
    }

    [Fact]
    public void KeyPressed_WhileSleeping_TransitionsToWaking_AndThenIdle()
    {
        var sut = new AvatarStateMachine { SleepAfter = TimeSpan.FromMinutes(10) };
        sut.Tick(idleFor: TimeSpan.FromMinutes(10), activeFor: TimeSpan.Zero);
        Assert.Equal(AvatarState.Sleeping, sut.CurrentState);

        sut.Apply(AvatarInput.KeyPressed);
        Assert.Equal(AvatarState.Waking, sut.CurrentState);

        sut.Apply(AvatarInput.AnimationCompleted);
        Assert.Equal(AvatarState.Idle, sut.CurrentState);
    }

    [Fact]
    public void Tick_ShortPause_PlaysARandomIdleGesture_Once()
    {
        var sut = new AvatarStateMachine(randomGesturePicker: () => 1); // SippingCoffee

        sut.Tick(idleFor: TimeSpan.FromSeconds(20), activeFor: TimeSpan.Zero);
        Assert.Equal(AvatarState.SippingCoffee, sut.CurrentState);

        // Ticking again while still idle shouldn't retrigger the gesture.
        sut.Tick(idleFor: TimeSpan.FromSeconds(21), activeFor: TimeSpan.Zero);
        Assert.Equal(AvatarState.SippingCoffee, sut.CurrentState);
    }

    [Fact]
    public void Tick_LongContinuousActivity_YawnsThenSuggestsBreak()
    {
        var sut = new AvatarStateMachine { BreakAfter = TimeSpan.FromMinutes(50) };

        sut.Tick(idleFor: TimeSpan.Zero, activeFor: TimeSpan.FromMinutes(50));
        Assert.Equal(AvatarState.Yawning, sut.CurrentState);

        sut.Apply(AvatarInput.AnimationCompleted);
        Assert.Equal(AvatarState.SuggestingBreak, sut.CurrentState);
    }

    [Fact]
    public void Tick_SuppressBreakSuggestions_SkipsYawnButStillSleeps()
    {
        var sut = new AvatarStateMachine
        {
            BreakAfter = TimeSpan.FromMinutes(50),
            SleepAfter = TimeSpan.FromMinutes(10),
            SuppressBreakSuggestions = true,
        };

        sut.Tick(idleFor: TimeSpan.Zero, activeFor: TimeSpan.FromMinutes(50));
        Assert.Equal(AvatarState.Idle, sut.CurrentState);

        sut.Tick(idleFor: TimeSpan.FromMinutes(10), activeFor: TimeSpan.FromMinutes(50));
        Assert.Equal(AvatarState.Sleeping, sut.CurrentState);
    }

    [Theory]
    [InlineData(AvatarInput.BreakAccepted)]
    [InlineData(AvatarInput.BreakSnoozed)]
    public void SuggestingBreak_RespondingToPrompt_ReturnsToIdle(AvatarInput response)
    {
        var sut = new AvatarStateMachine { BreakAfter = TimeSpan.FromMinutes(50) };
        sut.Tick(idleFor: TimeSpan.Zero, activeFor: TimeSpan.FromMinutes(50));
        sut.Apply(AvatarInput.AnimationCompleted);
        Assert.Equal(AvatarState.SuggestingBreak, sut.CurrentState);

        sut.Apply(response);

        Assert.Equal(AvatarState.Idle, sut.CurrentState);
    }

    [Fact]
    public void ReminderDue_FromIdle_TransitionsToAlerting_AndDismissReturnsToIdle()
    {
        var sut = new AvatarStateMachine();

        sut.Apply(AvatarInput.ReminderDue);
        Assert.Equal(AvatarState.Alerting, sut.CurrentState);

        sut.Apply(AvatarInput.AlertDismissed);
        Assert.Equal(AvatarState.Idle, sut.CurrentState);
    }

    [Fact]
    public void ReminderDue_WhileHeld_DoesNotInterruptTheDrag()
    {
        var sut = new AvatarStateMachine();
        sut.Apply(AvatarInput.DragStarted);

        sut.Apply(AvatarInput.ReminderDue);

        Assert.Equal(AvatarState.Held, sut.CurrentState);
    }

    [Fact]
    public void ReminderDue_WhileAlreadyAlerting_DoesNotRestartTheAlert()
    {
        var sut = new AvatarStateMachine();
        sut.Apply(AvatarInput.ReminderDue);

        sut.Apply(AvatarInput.ReminderDue);

        Assert.Equal(AvatarState.Alerting, sut.CurrentState);
    }
}
