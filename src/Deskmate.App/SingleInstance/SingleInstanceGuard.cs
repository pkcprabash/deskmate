using System;
using System.IO;
using Deskmate.Infrastructure;

namespace Deskmate.App.SingleInstance;

/// <summary>
/// Cross-platform single-instance guard backed by an exclusively-held lock
/// file (System.Threading.Mutex isn't portable across Windows/macOS in the
/// way this app needs).
/// </summary>
public sealed class SingleInstanceGuard : IDisposable
{
    private readonly FileStream? _lockStream;

    private SingleInstanceGuard(FileStream? lockStream)
    {
        _lockStream = lockStream;
    }

    public bool IsPrimaryInstance => _lockStream is not null;

    public static SingleInstanceGuard TryAcquire()
    {
        var directory = AppPaths.GetAppDataDirectory();
        Directory.CreateDirectory(directory);
        var lockFilePath = Path.Combine(directory, "deskmate.lock");

        try
        {
            var stream = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return new SingleInstanceGuard(stream);
        }
        catch (IOException)
        {
            return new SingleInstanceGuard(null);
        }
    }

    public void Dispose() => _lockStream?.Dispose();
}
