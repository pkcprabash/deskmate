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
}
