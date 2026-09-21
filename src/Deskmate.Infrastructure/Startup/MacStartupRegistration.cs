using System;
using System.IO;
using System.Threading.Tasks;

namespace Deskmate.Infrastructure.Startup;

/// <summary>macOS implementation: a LaunchAgent plist installed to ~/Library/LaunchAgents.</summary>
public sealed class MacStartupRegistration : IStartupRegistration
{
    private const string AgentLabel = "com.deskmate.app";

    private static string PlistPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", $"{AgentLabel}.plist");

    public Task EnableAsync()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executablePath))
        {
            return Task.CompletedTask;
        }

        var plist = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
                <key>Label</key>
                <string>{AgentLabel}</string>
                <key>ProgramArguments</key>
                <array>
                    <string>{executablePath}</string>
                    <string>--startup</string>
                </array>
                <key>RunAtLoad</key>
                <true/>
            </dict>
            </plist>
            """;

        Directory.CreateDirectory(Path.GetDirectoryName(PlistPath)!);
        File.WriteAllText(PlistPath, plist);
        return Task.CompletedTask;
    }

    public Task DisableAsync()
    {
        if (File.Exists(PlistPath))
        {
            File.Delete(PlistPath);
        }

        return Task.CompletedTask;
    }
}
