using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Tmds.DBus.Protocol;

namespace Deskmate.Infrastructure.Activity;

/// <summary>
/// Linux implementation of <see cref="ISessionEventsMonitor"/>. Watches systemd-logind's
/// <c>PrepareForSleep</c> signal on the system bus: it fires with <c>true</c> just before
/// suspend and <c>false</c> right after resume, which is what wakes the avatar. Screen
/// lock/unlock has no single standard signal across desktop environments, so unlike
/// Windows and macOS this only covers sleep/wake, not lock/unlock.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxSessionEventsMonitor : ISessionEventsMonitor, IHostedService, IDisposable
{
    private const string LoginService = "org.freedesktop.login1";
    private const string LoginPath = "/org/freedesktop/login1";
    private const string LoginManagerInterface = "org.freedesktop.login1.Manager";

    private IDisposable? _subscription;

    public event EventHandler? SessionResumed;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _subscription = await DBusConnection.System.WatchSignalAsync(
                sender: LoginService,
                path: LoginPath,
                @interface: LoginManagerInterface,
                signal: "PrepareForSleep",
                reader: static (Message message, object? state) => message.GetBodyReader().ReadBool(),
                handler: OnPrepareForSleep,
                flags: ObserverFlags.None);
        }
        catch (Exception)
        {
            // No system bus, or logind isn't running (e.g. a container or minimal desktop):
            // the avatar still wakes on the next keypress, just not immediately on resume.
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    private void OnPrepareForSleep(Notification<bool> notification)
    {
        // false = resuming from sleep, true = about to sleep.
        if (notification.HasValue && notification.Value == false)
        {
            SessionResumed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose() => _subscription?.Dispose();
}
