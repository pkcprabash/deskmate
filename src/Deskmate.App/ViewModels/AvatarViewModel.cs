using System;
using System.Threading;
using System.Threading.Tasks;
using Deskmate.App.Avatars;
using Deskmate.Core;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;

namespace Deskmate.App.ViewModels;

public class AvatarViewModel(SettingsService settingsService, AvatarPackLoader avatarPackLoader) : ViewModelBase
{
    private readonly AvatarStateMachine _stateMachine = new();

    private int _settingsId;

    public double? SavedPositionX { get; private set; }
    public double? SavedPositionY { get; private set; }
    public LoadedAvatarPack? Pack { get; private set; }
    public AvatarState CurrentState => _stateMachine.CurrentState;

    public event Action<AvatarState>? StateChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetOrCreateAsync(cancellationToken);
        _settingsId = settings.Id;
        SavedPositionX = settings.PositionX;
        SavedPositionY = settings.PositionY;
        Pack = LoadedAvatarPack.Load(avatarPackLoader, settings.AvatarPack);
    }

    public Task SavePositionAsync(double x, double y, CancellationToken cancellationToken = default) =>
        settingsService.SavePositionAsync(_settingsId, x, y, cancellationToken);

    public void NotifyClicked() => Transition(AvatarInput.Clicked);

    public void NotifyDragStarted() => Transition(AvatarInput.DragStarted);

    public void NotifyDragEnded() => Transition(AvatarInput.DragEnded);

    public void NotifyAnimationCompleted() => Transition(AvatarInput.AnimationCompleted);

    private void Transition(AvatarInput input)
    {
        _stateMachine.Apply(input);
        StateChanged?.Invoke(_stateMachine.CurrentState);
    }
}
