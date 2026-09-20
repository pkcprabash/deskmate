using Avalonia;
using Deskmate.App.Hosting;
using Deskmate.App.SingleInstance;
using Deskmate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Deskmate.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var singleInstanceGuard = SingleInstanceGuard.TryAcquire();
        if (!singleInstanceGuard.IsPrimaryInstance)
        {
            Console.Error.WriteLine("Deskmate is already running.");
            return;
        }

        using IHost host = AppHost.CreateHostBuilder(args).Build();

        using (var dbContext = host.Services.GetRequiredService<IDbContextFactory<DeskmateDbContext>>().CreateDbContext())
        {
            dbContext.Database.Migrate();
        }

        host.Start();
        App.Host = host;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            host.StopAsync().GetAwaiter().GetResult();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
