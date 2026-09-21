using Deskmate.Infrastructure.Activity;
using Deskmate.Infrastructure.Avatars;
using Deskmate.Infrastructure.Data;
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
                services.AddHostedService<StartupHostedService>();
            });
}
