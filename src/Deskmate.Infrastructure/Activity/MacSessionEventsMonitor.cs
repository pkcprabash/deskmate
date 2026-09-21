using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Deskmate.Infrastructure.Activity;

/// <summary>
/// macOS implementation of <see cref="ISessionEventsMonitor"/>. Observes NSWorkspace's
/// wake-from-sleep and session-became-active notifications via the Objective-C runtime
/// directly, since no Xamarin.Mac/Mac Catalyst binding is available on a plain net10.0
/// target. Avalonia's macOS backend already runs a real NSApplication run loop, so these
/// notifications deliver normally.
/// </summary>
public sealed class MacSessionEventsMonitor : ISessionEventsMonitor, IHostedService, IDisposable
{
    private const string ObjCLibrary = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_ret(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_ret(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_ret(
        IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string arg1);

    [DllImport(ObjCLibrary, EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_addObserver(
        IntPtr receiver, IntPtr selector, IntPtr observer, IntPtr forSelector, IntPtr name, IntPtr obj);

    [DllImport(ObjCLibrary)]
    private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, nint extraBytes);

    [DllImport(ObjCLibrary)]
    private static extern void objc_registerClassPair(IntPtr cls);

    [DllImport(ObjCLibrary)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr selector, IntPtr impl, string types);

    private delegate void NotificationHandler(IntPtr self, IntPtr cmd, IntPtr notification);

    // Kept alive for the monitor's lifetime: the unmanaged function pointer below points
    // into this delegate, so it must not be collected while the Objective-C class can
    // still call it.
    private readonly NotificationHandler _handler;
    private readonly IntPtr _observer;
    private IntPtr _notificationCenter;

    public event EventHandler? SessionResumed;

    public MacSessionEventsMonitor()
    {
        _handler = OnNotification;

        var observerClass = objc_allocateClassPair(
            objc_getClass("NSObject"), $"DeskmateSessionObserver_{Guid.NewGuid():N}", 0);
        class_addMethod(
            observerClass,
            sel_registerName("handleNotification:"),
            Marshal.GetFunctionPointerForDelegate(_handler),
            "v@:@");
        objc_registerClassPair(observerClass);

        var alloc = objc_msgSend_ret(observerClass, sel_registerName("alloc"));
        _observer = objc_msgSend_ret(alloc, sel_registerName("init"));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var workspace = objc_msgSend_ret(objc_getClass("NSWorkspace"), sel_registerName("sharedWorkspace"));
        _notificationCenter = objc_msgSend_ret(workspace, sel_registerName("notificationCenter"));

        var handleSelector = sel_registerName("handleNotification:");
        Observe("NSWorkspaceDidWakeNotification", handleSelector);
        Observe("NSWorkspaceSessionDidBecomeActiveNotification", handleSelector);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_notificationCenter != IntPtr.Zero)
        {
            objc_msgSend_ret(_notificationCenter, sel_registerName("removeObserver:"), _observer);
        }

        return Task.CompletedTask;
    }

    private void Observe(string notificationName, IntPtr handleSelector)
    {
        var name = objc_msgSend_ret(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), notificationName);
        objc_msgSend_addObserver(
            _notificationCenter,
            sel_registerName("addObserver:selector:name:object:"),
            _observer,
            handleSelector,
            name,
            IntPtr.Zero);
    }

    private void OnNotification(IntPtr self, IntPtr cmd, IntPtr notification) =>
        SessionResumed?.Invoke(this, EventArgs.Empty);

    public void Dispose()
    {
    }
}
