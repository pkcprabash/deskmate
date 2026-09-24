using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Deskmate.Infrastructure.Display;

/// <summary>
/// Windows implementation: the foreground window is "full-screen" when its bounds
/// exactly cover the monitor it's on (a maximized window still leaves the taskbar showing,
/// so this doesn't false-positive on that).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsFullScreenDetector : IFullScreenDetector
{
    private const uint MonitorDefaultToNearest = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    public bool IsFullScreenAppActive()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == GetDesktopWindow() || foreground == GetShellWindow())
        {
            return false;
        }

        if (!GetWindowRect(foreground, out var windowRect))
        {
            return false;
        }

        var monitor = MonitorFromWindow(foreground, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return false;
        }

        var screen = monitorInfo.Monitor;
        return windowRect.Left <= screen.Left && windowRect.Top <= screen.Top
            && windowRect.Right >= screen.Right && windowRect.Bottom >= screen.Bottom;
    }
}
