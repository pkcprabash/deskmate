using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Deskmate.App.ViewModels;
using Deskmate.App.Views;
using Deskmate.Infrastructure.Activity;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;
using Deskmate.Infrastructure.Startup;
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
            var startupRegistration = Host!.Services.GetRequiredService<IStartupRegistration>();

            // First-run setup is quick enough (a single local SQLite read) that blocking
            // here is simpler than threading an async gate through Avalonia's startup.
            var settings = settingsService.GetOrCreateAsync().GetAwaiter().GetResult();

            if (!settings.FirstRunCompleted)
            {
                var firstRun = new FirstRunWindow
                {
                    SettingsService = settingsService,
                    StartupRegistration = startupRegistration,
                    SettingsId = settings.Id,
                    DefaultAvatarPack = settings.AvatarPack,
                };
                firstRun.Completed += () =>
                {
                    var avatarWindow = CreateAvatarWindow(settingsService, startupRegistration);
                    avatarWindow.Show();
                    desktop.MainWindow = avatarWindow;
                    firstRun.Close();
                };
                desktop.MainWindow = firstRun;
            }
            else
            {
                desktop.MainWindow = CreateAvatarWindow(settingsService, startupRegistration);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private AvatarWindow CreateAvatarWindow(SettingsService settingsService, IStartupRegistration startupRegistration)
    {
        var avatarPackLoader = Host!.Services.GetRequiredService<AvatarPackLoader>();
        var idleMonitor = Host!.Services.GetRequiredService<IdleMonitor>();
        var sessionEventsMonitor = Host!.Services.GetRequiredService<ISessionEventsMonitor>();

        return new AvatarWindow
        {
            SettingsService = settingsService,
            StartupRegistration = startupRegistration,
            DataContext = new AvatarViewModel(settingsService, avatarPackLoader, idleMonitor, sessionEventsMonitor),
        };
    }

    private void OnQuitClicked(object? sender, EventArgs e)
    {
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
