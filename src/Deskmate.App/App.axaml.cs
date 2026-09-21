using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Deskmate.App.ViewModels;
using Deskmate.App.Views;
using Deskmate.Infrastructure.Activity;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Deskmate.App;

public partial class App : Application
{
    public static IHost? Host { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settingsService = Host!.Services.GetRequiredService<SettingsService>();
            var avatarPackLoader = Host!.Services.GetRequiredService<AvatarPackLoader>();
            var idleMonitor = Host!.Services.GetRequiredService<IdleMonitor>();
            var sessionEventsMonitor = Host!.Services.GetRequiredService<ISessionEventsMonitor>();
            desktop.MainWindow = new AvatarWindow
            {
                DataContext = new AvatarViewModel(settingsService, avatarPackLoader, idleMonitor, sessionEventsMonitor),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}