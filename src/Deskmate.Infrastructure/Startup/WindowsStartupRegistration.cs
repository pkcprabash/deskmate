using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Deskmate.Infrastructure.Startup;

/// <summary>Windows implementation: a value under HKCU's Run key (no admin rights needed).</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsStartupRegistration : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Deskmate";

    public Task EnableAsync()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            return Task.CompletedTask;
        }

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, $"\"{executablePath}\" --startup");
        return Task.CompletedTask;
    }

    public Task DisableAsync()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
        return Task.CompletedTask;
    }
}
