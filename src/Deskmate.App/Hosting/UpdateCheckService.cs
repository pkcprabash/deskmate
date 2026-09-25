using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace Deskmate.App.Hosting;

/// <summary>
/// Checks GitHub Releases for a newer version once at startup and then every few hours,
/// downloading it in the background. The update is applied the next time Deskmate starts,
/// so it never interrupts the avatar. Does nothing when running from source (not installed).
/// </summary>
public sealed class UpdateCheckService(ILogger<UpdateCheckService> logger) : BackgroundService
{
    private const string RepositoryUrl = "https://github.com/pkcprabash/deskmate";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var manager = new UpdateManager(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
        if (!manager.IsInstalled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var update = await manager.CheckForUpdatesAsync();
                if (update is not null)
                {
                    logger.LogInformation("Downloading update {Version}.", update.TargetFullRelease.Version);
                    await manager.DownloadUpdatesAsync(update);
                }
            }
            catch (Exception ex)
            {
                // Offline or rate-limited: try again at the next interval.
                logger.LogWarning(ex, "Update check failed.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
