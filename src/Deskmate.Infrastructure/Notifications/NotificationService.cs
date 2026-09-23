using System;
using System.Threading.Tasks;
using DesktopNotifications;
using DesktopNotifications.Apple;
using DesktopNotifications.Windows;
using Microsoft.Extensions.Logging;

namespace Deskmate.Infrastructure.Notifications;

/// <summary>
/// Native OS notification fallback for reminder alerts, for when the avatar may not be
/// visible (e.g. a full-screen app). Every call is wrapped in try/catch: showing a real
/// macOS banner needs a signed app bundle, which a plain `dotnet run` executable doesn't
/// have, so failures here are expected in dev and must never crash the app.
/// </summary>
public sealed class NotificationService : IDisposable
{
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationManager? _manager;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        _manager = CreateManager();
    }

    public async Task InitializeAsync()
    {
        if (_manager is null)
        {
            return;
        }

        try
        {
            await _manager.Initialize();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize native notifications.");
        }
    }

    public async Task ShowAsync(string title, string body)
    {
        if (_manager is null)
        {
            return;
        }

        try
        {
            await _manager.ShowNotification(new Notification { Title = title, Body = body }, expirationTime: null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not show a native notification.");
        }
    }

    private static INotificationManager? CreateManager()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsNotificationManager(WindowsApplicationContext.FromCurrentProcess("Deskmate", "Deskmate.App"));
        }

        if (OperatingSystem.IsMacOS())
        {
            return new AppleNotificationManager();
        }

        return null;
    }

    public void Dispose() => (_manager as IDisposable)?.Dispose();
}
