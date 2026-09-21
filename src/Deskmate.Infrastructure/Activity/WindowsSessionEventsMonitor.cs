using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace Deskmate.Infrastructure.Activity;

/// <summary>Windows implementation of <see cref="ISessionEventsMonitor"/> using <see cref="SystemEvents"/>.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsSessionEventsMonitor : ISessionEventsMonitor, IHostedService, IDisposable
{
    public event EventHandler? SessionResumed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        return Task.CompletedTask;
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            SessionResumed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            SessionResumed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}
