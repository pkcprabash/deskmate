using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Deskmate.App.Hosting;

/// <summary>
/// Placeholder hosted service confirming the generic host starts and stops
/// alongside the Avalonia app. Later background services (monitors,
/// scheduler) will follow this same pattern.
/// </summary>
public sealed class StartupHostedService(ILogger<StartupHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deskmate host started.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Deskmate host stopping.");
        return Task.CompletedTask;
    }
}
