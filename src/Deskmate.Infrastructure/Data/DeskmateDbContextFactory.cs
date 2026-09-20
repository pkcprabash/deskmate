using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Deskmate.Infrastructure.Data;

/// <summary>
/// Lets `dotnet ef` create a DeskmateDbContext at design time (e.g. for
/// migrations), independent of the app's own DI-configured instance.
/// </summary>
public class DeskmateDbContextFactory : IDesignTimeDbContextFactory<DeskmateDbContext>
{
    public DeskmateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DeskmateDbContext>()
            .UseSqlite($"Data Source={DeskmateDbContext.GetDatabasePath()}");

        return new DeskmateDbContext(optionsBuilder.Options);
    }
}
