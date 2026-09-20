using System.Threading;
using System.Threading.Tasks;
using Deskmate.Infrastructure.Data;

namespace Deskmate.App.ViewModels;

public class AvatarViewModel(SettingsService settingsService) : ViewModelBase
{
    private int _settingsId;

    public double? SavedPositionX { get; private set; }
    public double? SavedPositionY { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetOrCreateAsync(cancellationToken);
        _settingsId = settings.Id;
        SavedPositionX = settings.PositionX;
        SavedPositionY = settings.PositionY;
    }

    public Task SavePositionAsync(double x, double y, CancellationToken cancellationToken = default) =>
        settingsService.SavePositionAsync(_settingsId, x, y, cancellationToken);
}
