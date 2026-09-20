using System.Threading;
using System.Threading.Tasks;
using Deskmate.Infrastructure.Avatars;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Deskmate.App.Hosting;

/// <summary>
/// Placeholder hosted service confirming the generic host starts and stops
/// alongside the Avalonia app. Later background services (monitors,
/// scheduler) will follow this same pattern.
/// </summary>
public sealed class StartupHostedService(ILogger<StartupHostedService> logger, AvatarPackLoader avatarPackLoader) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deskmate host started.");

        var pack = avatarPackLoader.Load("mint");
        logger.LogInformation("Loaded avatar pack '{PackName}' with {AnimationCount} animation(s).", pack.Name, pack.Animations.Count);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deskmate host stopping.");
        return Task.CompletedTask;
    }
}
