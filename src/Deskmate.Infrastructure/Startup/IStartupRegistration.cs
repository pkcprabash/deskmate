using System.Threading.Tasks;

namespace Deskmate.Infrastructure.Startup;

/// <summary>
/// Registers (or unregisters) Deskmate to launch automatically when the user
/// logs in. One implementation per OS (<see cref="WindowsStartupRegistration"/>,
/// <see cref="MacStartupRegistration"/>), chosen at startup in AppHost.
/// </summary>
public interface IStartupRegistration
{
    Task EnableAsync();

    Task DisableAsync();
}
