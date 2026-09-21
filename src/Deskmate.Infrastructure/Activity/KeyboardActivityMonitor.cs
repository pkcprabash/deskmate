using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharpHook;
using SharpHook.Data;

namespace Deskmate.Infrastructure.Activity;

/// <summary>
/// Counts keypresses via a global keyboard hook without ever reading which
/// key was pressed — only that one was, and when. On macOS the process
/// needs the user to grant Input Monitoring permission in System Settings;
/// without it the hook silently receives no events, so callers should watch
/// <see cref="LastActivityAtUtc"/> to notice.
/// </summary>
public class KeyboardActivityMonitor : IHostedService, IDisposable
{
    private readonly ILogger<KeyboardActivityMonitor> _logger;
    private readonly IGlobalHook _hook;
    private long _keyPressCount;

    public KeyboardActivityMonitor(ILogger<KeyboardActivityMonitor> logger)
    {
        _logger = logger;
        _hook = new SimpleGlobalHook();
        _hook.KeyPressed += OnKeyPressed;
    }

    public long KeyPressCount => Interlocked.Read(ref _keyPressCount);

    public DateTimeOffset LastActivityAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        // Intentionally not touching e.Data (which key) — count and timestamp only.
        Interlocked.Increment(ref _keyPressCount);
        LastActivityAtUtc = DateTimeOffset.UtcNow;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // RunAsync's task only completes when the hook stops, so it must not be
            // awaited here — doing so would hang the host's startup indefinitely.
            var runTask = _hook.RunAsync(GlobalHookType.Keyboard, useBackgroundThread: true);
            _ = runTask.ContinueWith(
                t => LogStartupFailure(t.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            _logger.LogInformation("Keyboard activity monitor started.");
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex);
        }

        return Task.CompletedTask;
    }

    private void LogStartupFailure(Exception? exception) =>
        _logger.LogWarning(
            exception,
            "Could not start the keyboard activity monitor. On macOS, grant Input Monitoring " +
            "permission in System Settings > Privacy & Security > Input Monitoring, then relaunch Deskmate.");

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _hook.Stop();
        return Task.CompletedTask;
    }

    public void Dispose() => _hook.Dispose();
}
