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
                services.AddDbContext<DeskmateDbContext>(options =>
                    options.UseSqlite($"Data Source={DeskmateDbContext.GetDatabasePath()}"));
                services.AddHostedService<StartupHostedService>();
            });
}
