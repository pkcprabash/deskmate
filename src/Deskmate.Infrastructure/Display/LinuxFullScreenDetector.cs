using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Deskmate.Infrastructure.Display;

/// <summary>
/// Linux implementation, via Xlib and the EWMH conventions most window managers follow:
/// reads the root window's <c>_NET_ACTIVE_WINDOW</c>, then checks whether that window's
/// <c>_NET_WM_STATE</c> includes <c>_NET_WM_STATE_FULLSCREEN</c>. Only works under X11 (including
/// XWayland); on a pure-Wayland session with no X server, opening the display fails and this
/// always reports false rather than throwing — the caller already treats detection failures
/// as "not full-screen" (see AvatarWindow.CheckFullScreen).
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxFullScreenDetector : IFullScreenDetector, IDisposable
{
    private const string X11Library = "libX11.so.6";

    // Xlib's "format 32" properties are stored one native `long` per item, not one 32-bit
    // int per item — a long-standing Xlib ABI quirk, true on both 32- and 64-bit Linux,
    // where IntPtr.Size matches C's `long` size.
    private static readonly int LongSize = IntPtr.Size;

    private const int PropertySuccess = 0;
    private const long AnyPropertyType = 0;
    private const long MaxLongsToRead = 32;

    [DllImport(X11Library)]
    private static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport(X11Library)]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport(X11Library)]
    private static extern UIntPtr XDefaultRootWindow(IntPtr display);

    [DllImport(X11Library, CharSet = CharSet.Ansi)]
    private static extern UIntPtr XInternAtom(IntPtr display, string atomName, [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [DllImport(X11Library)]
    private static extern int XGetWindowProperty(
        IntPtr display, UIntPtr window, UIntPtr property,
        IntPtr longOffset, IntPtr longLength, [MarshalAs(UnmanagedType.Bool)] bool delete,
        UIntPtr requestType, out UIntPtr actualTypeReturn, out int actualFormatReturn,
        out UIntPtr numberOfItemsReturn, out UIntPtr bytesAfterReturn, out IntPtr propertyReturn);

    [DllImport(X11Library)]
    private static extern int XFree(IntPtr data);

    private bool _triedOpen;
    private IntPtr _display;

    public bool IsFullScreenAppActive()
    {
        var display = GetDisplay();
        if (display == IntPtr.Zero)
        {
            return false;
        }

        var root = XDefaultRootWindow(display);
        var activeWindowAtom = XInternAtom(display, "_NET_ACTIVE_WINDOW", true);
        if (!TryReadWindowProperty(display, root, activeWindowAtom, out var activeWindow) || activeWindow == UIntPtr.Zero)
        {
            return false;
        }

        var stateAtom = XInternAtom(display, "_NET_WM_STATE", true);
        var fullscreenAtom = XInternAtom(display, "_NET_WM_STATE_FULLSCREEN", true);
        return TryReadAtomListContains(display, activeWindow, stateAtom, fullscreenAtom);
    }

    /// <summary>Opens the X display once and reuses it; null/zero forever after a failed attempt, so we don't retry every tick.</summary>
    private IntPtr GetDisplay()
    {
        if (_triedOpen)
        {
            return _display;
        }

        _triedOpen = true;
        try
        {
            _display = XOpenDisplay(IntPtr.Zero);
        }
        catch (DllNotFoundException)
        {
            _display = IntPtr.Zero;
        }

        return _display;
    }

    private static bool TryReadWindowProperty(IntPtr display, UIntPtr window, UIntPtr atom, out UIntPtr value)
    {
        value = UIntPtr.Zero;

        var status = XGetWindowProperty(
            display, window, atom, IntPtr.Zero, (IntPtr)MaxLongsToRead, false, (UIntPtr)AnyPropertyType,
            out _, out var format, out var itemCount, out _, out var data);

        if (status != PropertySuccess || data == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            if (format != 32 || itemCount == UIntPtr.Zero)
            {
                return false;
            }

            value = (UIntPtr)(ulong)Marshal.ReadIntPtr(data, 0);
            return true;
        }
        finally
        {
            XFree(data);
        }
    }

    private static bool TryReadAtomListContains(IntPtr display, UIntPtr window, UIntPtr atom, UIntPtr target)
    {
        var status = XGetWindowProperty(
            display, window, atom, IntPtr.Zero, (IntPtr)MaxLongsToRead, false, (UIntPtr)AnyPropertyType,
            out _, out var format, out var itemCount, out _, out var data);

        if (status != PropertySuccess || data == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            if (format != 32)
            {
                return false;
            }

            var count = Math.Min((long)itemCount, MaxLongsToRead);
            for (var i = 0; i < count; i++)
            {
                var item = (UIntPtr)(ulong)Marshal.ReadIntPtr(data, i * LongSize);
                if (item == target)
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            XFree(data);
        }
    }

    public void Dispose()
    {
        if (_display != IntPtr.Zero)
        {
            XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
    }
}
