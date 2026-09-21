using System.Collections.Generic;

namespace Deskmate.Core;

/// <summary>
/// No UI dependencies, so it can be fully unit-tested. One-shot states
/// (Stretching, SippingCoffee, LookingAround, Yawning, Waving, Waking) return
/// to Idle when their animation ends; SuggestingBreak and Alerting stay
/// until the user responds.
/// </summary>
public class AvatarStateMachine
{
    private static readonly HashSet<AvatarState> OneShotStates = new()
    {
        AvatarState.Stretching,
        AvatarState.SippingCoffee,
        AvatarState.LookingAround,
        AvatarState.Yawning,
        AvatarState.Waving,
        AvatarState.Waking,
    };

    public AvatarState CurrentState { get; private set; } = AvatarState.Idle;

    public void Apply(AvatarInput input)
    {
        CurrentState = input switch
        {
            AvatarInput.AnimationCompleted when OneShotStates.Contains(CurrentState) => AvatarState.Idle,
            AvatarInput.Clicked when CurrentState is AvatarState.Idle or AvatarState.Typing => AvatarState.Waving,
            AvatarInput.DragStarted => AvatarState.Held,
            AvatarInput.DragEnded when CurrentState == AvatarState.Held => AvatarState.Idle,
            _ => CurrentState,
        };
    }
}
