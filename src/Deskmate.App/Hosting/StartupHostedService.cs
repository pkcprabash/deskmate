using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Deskmate.App.Hosting;

/// <summary>
/// Placeholder hosted service confirming the generic host starts and stops
/// alongside the Avalonia app. Later background services (monitors,
/// scheduler) will follow this same pattern.
/// </summary>
public sealed class StartupHostedService(
    ILogger<StartupHostedService> logger,
    AvatarPackLoader avatarPackLoader,
    NotificationService notificationService) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deskmate host started.");

        var packsDirectory = AvatarPackLoader.GetPacksDirectory();
        if (Directory.Exists(packsDirectory))
        {
            foreach (var packPath in Directory.GetDirectories(packsDirectory))
            {
                var packName = Path.GetFileName(packPath);
                if (avatarPackLoader.TryLoad(packName, out var pack, out var error))
                {
                    logger.LogInformation("Loaded avatar pack '{PackName}' with {AnimationCount} animation(s).", pack!.Name, pack.Animations.Count);
                }
                else
                {
                    logger.LogWarning("Skipping avatar pack '{PackName}'. {Problems}", packName, error!.Message);
                }
            }
        }

        await notificationService.InitializeAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deskmate host stopping.");
        return Task.CompletedTask;
    }
}
