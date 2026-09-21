using System;
using System.Collections.Generic;

namespace Deskmate.Core;

/// <summary>
/// No UI dependencies, so it can be fully unit-tested. One-shot states
/// (Stretching, SippingCoffee, LookingAround, Yawning, Waving, Waking) return
/// to Idle when their animation ends, except Yawning, which hands off to
/// SuggestingBreak; SuggestingBreak and Alerting stay until the user responds.
/// </summary>
public class AvatarStateMachine(Func<int>? randomGesturePicker = null)
{
    private static readonly AvatarState[] IdlePauseGestures =
    [
        AvatarState.Stretching,
        AvatarState.SippingCoffee,
        AvatarState.LookingAround,
    ];

    private static readonly HashSet<AvatarState> OneShotStates = new()
    {
        AvatarState.Stretching,
        AvatarState.SippingCoffee,
        AvatarState.LookingAround,
        AvatarState.Yawning,
        AvatarState.Waving,
        AvatarState.Waking,
    };

    private static readonly TimeSpan PauseGestureAfter = TimeSpan.FromSeconds(20);

    private readonly Func<int> _pickGesture = randomGesturePicker ?? (() => Random.Shared.Next(IdlePauseGestures.Length));

    private bool _pauseGesturePlayed;

    public AvatarState CurrentState { get; private set; } = AvatarState.Idle;

    /// <summary>How long the user can be idle before the avatar falls asleep.</summary>
    public TimeSpan SleepAfter { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>How long the avatar lets you work before suggesting a break.</summary>
    public TimeSpan BreakAfter { get; set; } = TimeSpan.FromMinutes(50);

    public void Apply(AvatarInput input)
    {
        CurrentState = input switch
        {
            AvatarInput.AnimationCompleted when CurrentState == AvatarState.Yawning => AvatarState.SuggestingBreak,
            AvatarInput.AnimationCompleted when OneShotStates.Contains(CurrentState) => AvatarState.Idle,
            AvatarInput.Clicked when CurrentState is AvatarState.Idle or AvatarState.Typing => AvatarState.Waving,
            AvatarInput.DragStarted => AvatarState.Held,
            AvatarInput.DragEnded when CurrentState == AvatarState.Held => AvatarState.Idle,
            AvatarInput.KeyPressed when CurrentState == AvatarState.Sleeping => AvatarState.Waking,
            AvatarInput.KeyPressed when IsRestable(CurrentState) => AvatarState.Typing,
            AvatarInput.BreakAccepted when CurrentState == AvatarState.SuggestingBreak => AvatarState.Idle,
            AvatarInput.BreakSnoozed when CurrentState == AvatarState.SuggestingBreak => AvatarState.Idle,
            _ => CurrentState,
        };

        if (input == AvatarInput.KeyPressed)
        {
            _pauseGesturePlayed = false;
        }
    }

    /// <summary>
    /// Advances time-driven behavior: falling asleep after <see cref="SleepAfter"/> of
    /// no activity, a random idle gesture after a short pause, and a break suggestion
    /// after <see cref="BreakAfter"/> of continuous activity. <paramref name="activeFor"/>
    /// is owned by the caller, which resets it whenever a break is taken.
    /// </summary>
    public void Tick(TimeSpan idleFor, TimeSpan activeFor)
    {
        if (!IsRestable(CurrentState))
        {
            return;
        }

        if (idleFor >= SleepAfter)
        {
            CurrentState = AvatarState.Sleeping;
            return;
        }

        if (idleFor >= PauseGestureAfter && !_pauseGesturePlayed)
        {
            _pauseGesturePlayed = true;
            CurrentState = IdlePauseGestures[_pickGesture() % IdlePauseGestures.Length];
            return;
        }

        if (activeFor >= BreakAfter)
        {
            CurrentState = AvatarState.Yawning;
        }
    }

    private static bool IsRestable(AvatarState state) => state is AvatarState.Idle or AvatarState.Typing;
}
