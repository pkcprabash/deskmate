using System;
using System.Runtime.InteropServices;

namespace Deskmate.Infrastructure.Display;

/// <summary>
/// macOS implementation: uses the public CGWindowListCopyWindowInfo API (unlike the
/// Accessibility API, this needs no extra permission) to find the frontmost normal-layer
/// window and checks whether it covers the whole main display — a real full-screen app's
/// window does; a merely-maximized one still leaves room for the menu bar.
/// </summary>
public sealed class MacFullScreenDetector : IFullScreenDetector
{
    private const string CoreGraphicsLibrary = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private const uint KCGWindowListOptionOnScreenOnly = 1;
    private const uint KCGWindowListExcludeDesktopElements = 16;
    private const uint KCGNullWindowID = 0;
    private const int KCFNumberDoubleType = 13;
    private const uint KCFStringEncodingUtf8 = 0x08000100;
    private const double SizeTolerance = 2.0; // rounding slack in reported window bounds

    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint
    {
        public double X;
        public double Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGSize
    {
        public double Width;
        public double Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CGRect
    {
        public CGPoint Origin;
        public CGSize Size;
    }

    [DllImport(CoreGraphicsLibrary)]
    private static extern uint CGMainDisplayID();

    [DllImport(CoreGraphicsLibrary)]
    private static extern CGRect CGDisplayBounds(uint display);

    [DllImport(CoreGraphicsLibrary)]
    private static extern IntPtr CGWindowListCopyWindowInfo(uint option, uint relativeToWindow);

    [DllImport(CoreFoundationLibrary)]
    private static extern nint CFArrayGetCount(IntPtr theArray);

    [DllImport(CoreFoundationLibrary)]
    private static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, nint idx);

    [DllImport(CoreFoundationLibrary)]
    private static extern IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

    [DllImport(CoreFoundationLibrary, CharSet = CharSet.Ansi)]
    private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, string cStr, uint encoding);

    [DllImport(CoreFoundationLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CFNumberGetValue(IntPtr number, int theType, out double valuePtr);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFRelease(IntPtr cf);

    public bool IsFullScreenAppActive()
    {
        var windowList = IntPtr.Zero;
        var boundsKey = IntPtr.Zero;
        var layerKey = IntPtr.Zero;
        var widthKey = IntPtr.Zero;
        var heightKey = IntPtr.Zero;

        try
        {
            var displayBounds = CGDisplayBounds(CGMainDisplayID());

            windowList = CGWindowListCopyWindowInfo(
                KCGWindowListOptionOnScreenOnly | KCGWindowListExcludeDesktopElements, KCGNullWindowID);
            if (windowList == IntPtr.Zero)
            {
                return false;
            }

            boundsKey = CFStringCreateWithCString(IntPtr.Zero, "kCGWindowBounds", KCFStringEncodingUtf8);
            layerKey = CFStringCreateWithCString(IntPtr.Zero, "kCGWindowLayer", KCFStringEncodingUtf8);
            widthKey = CFStringCreateWithCString(IntPtr.Zero, "Width", KCFStringEncodingUtf8);
            heightKey = CFStringCreateWithCString(IntPtr.Zero, "Height", KCFStringEncodingUtf8);

            var count = CFArrayGetCount(windowList);
            for (nint i = 0; i < count; i++)
            {
                var windowDict = CFArrayGetValueAtIndex(windowList, i);
                if (windowDict == IntPtr.Zero)
                {
                    continue;
                }

                var layerValue = CFDictionaryGetValue(windowDict, layerKey);
                if (layerValue == IntPtr.Zero || !CFNumberGetValue(layerValue, KCFNumberDoubleType, out var layer) || layer != 0)
                {
                    continue; // skip the menu bar, overlays, and other non-normal-layer windows
                }

                var boundsDict = CFDictionaryGetValue(windowDict, boundsKey);
                if (boundsDict == IntPtr.Zero)
                {
                    continue;
                }

                var widthValue = CFDictionaryGetValue(boundsDict, widthKey);
                var heightValue = CFDictionaryGetValue(boundsDict, heightKey);
                if (widthValue == IntPtr.Zero || heightValue == IntPtr.Zero
                    || !CFNumberGetValue(widthValue, KCFNumberDoubleType, out var width)
                    || !CFNumberGetValue(heightValue, KCFNumberDoubleType, out var height))
                {
                    continue;
                }

                // First normal-layer window in the (front-to-back ordered) list is the frontmost app window.
                return width >= displayBounds.Size.Width - SizeTolerance && height >= displayBounds.Size.Height - SizeTolerance;
            }

            return false;
        }
        finally
        {
            if (boundsKey != IntPtr.Zero) CFRelease(boundsKey);
            if (layerKey != IntPtr.Zero) CFRelease(layerKey);
            if (widthKey != IntPtr.Zero) CFRelease(widthKey);
            if (heightKey != IntPtr.Zero) CFRelease(heightKey);
            if (windowList != IntPtr.Zero) CFRelease(windowList);
        }
    }
}
