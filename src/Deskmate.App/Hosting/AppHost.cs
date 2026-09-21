using System;
using System.Runtime.Versioning;
using Deskmate.Infrastructure.Activity;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;
using Deskmate.Infrastructure.Startup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Deskmate.App.Hosting;

public static class AppHost
{
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddDbContextFactory<DeskmateDbContext>(options =>
                    options.UseSqlite($"Data Source={DeskmateDbContext.GetDatabasePath()}"));
                services.AddSingleton<SettingsService>();
                services.AddSingleton<AvatarPackLoader>();
                services.AddSingleton<KeyboardActivityMonitor>();
                services.AddHostedService(sp => sp.GetRequiredService<KeyboardActivityMonitor>());
                services.AddSingleton<IdleMonitor>();

                if (OperatingSystem.IsWindows())
                {
                    AddWindowsSessionEventsMonitor(services);
                    services.AddSingleton<IStartupRegistration, WindowsStartupRegistration>();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    services.AddSingleton<MacSessionEventsMonitor>();
                    services.AddSingleton<ISessionEventsMonitor>(sp => sp.GetRequiredService<MacSessionEventsMonitor>());
                    services.AddHostedService(sp => sp.GetRequiredService<MacSessionEventsMonitor>());
                    services.AddSingleton<IStartupRegistration, MacStartupRegistration>();
                }

                services.AddHostedService<StartupHostedService>();
            });

    [SupportedOSPlatform("windows")]
    private static void AddWindowsSessionEventsMonitor(IServiceCollection services)
    {
        services.AddSingleton<WindowsSessionEventsMonitor>();
        services.AddSingleton<ISessionEventsMonitor>(sp => sp.GetRequiredService<WindowsSessionEventsMonitor>());
        services.AddHostedService(sp => sp.GetRequiredService<WindowsSessionEventsMonitor>());
    }
}
