#if IOS
using System.Runtime.InteropServices;
using System.Text.Json;
using Foundation;
using ObjCRuntime;
using AmplitudeMauiPoc.Services;

namespace AmplitudeMauiPoc.Platforms.iOS;

/// <summary>
/// iOS implementation of <see cref="IAmplitudeService"/>. Calls into the
/// AmplitudeIOSBindings.xcframework (Swift wrapper around AmplitudeSwift +
/// AmplitudeSessionReplay) using objc_msgSend selectors.
///
/// The ObjC contract is defined in NativeLibraries/AmplitudeIOSBindings.
/// Keep selectors here in lockstep with the Swift wrapper.
/// </summary>
public sealed class AmplitudeIOSService : IAmplitudeService
{
    private const string ClassName = "AMPMauiAmplitude";
    private const string ServerZone = "US";       // matches Android (default zone)
    private const double SessionReplaySampleRate = 1.0;

    private const string LibObjC = "/usr/lib/libobjc.dylib";

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern void void_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern void void_objc_msgSend_IntPtr(
        IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern void void_objc_msgSend_IntPtr_IntPtr(
        IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    private static extern void void_objc_msgSend_IntPtr_IntPtr_Double(
        IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, double arg3);

    [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool bool_objc_msgSend(IntPtr receiver, IntPtr selector);

    private NSObject? _bridge;
    private bool _sessionReplayActive;

    public bool IsSessionReplayActive => _sessionReplayActive;

    public void Init(string apiKey)
    {
        try
        {
            var classHandle = Class.GetHandle(ClassName);
            if (classHandle == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Amplitude iOS] Class '{ClassName}' not found. " +
                    "Did you run NativeLibraries/AmplitudeIOSBindings/build-xcframework.sh " +
                    "and rebuild?");
                return;
            }

            // [[AMPMauiAmplitude alloc] init]
            var alloced = IntPtr_objc_msgSend(classHandle, Selector.GetHandle("alloc"));
            var inited = IntPtr_objc_msgSend(alloced, Selector.GetHandle("init"));
            _bridge = Runtime.GetNSObject(inited);

            if (_bridge is null)
            {
                System.Diagnostics.Debug.WriteLine("[Amplitude iOS] Failed to instantiate bridge.");
                return;
            }

            using var nsKey = new NSString(apiKey);
            using var nsZone = new NSString(ServerZone);
            void_objc_msgSend_IntPtr_IntPtr_Double(
                _bridge.Handle,
                Selector.GetHandle("initializeWithApiKey:serverZone:sampleRate:"),
                nsKey.Handle,
                nsZone.Handle,
                SessionReplaySampleRate);

            _sessionReplayActive = ReadIsSessionReplayActive();

            System.Diagnostics.Debug.WriteLine(
                $"[Amplitude iOS] Initialized. SR={_sessionReplayActive}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Amplitude iOS] Init failed: {ex}");
        }
    }

    public void Track(string eventName, Dictionary<string, object>? properties = null)
    {
        if (_bridge is null) return;
        // Serialize props on the calling thread (cheap), run objc_msgSend on a worker.
        var json = properties is null || properties.Count == 0
            ? string.Empty
            : JsonSerializer.Serialize(properties);
        var handle = _bridge.Handle;
        Task.Run(() =>
        {
            try
            {
                using var nsEvent = new NSString(eventName);
                using var nsJson = new NSString(json);
                void_objc_msgSend_IntPtr_IntPtr(
                    handle,
                    Selector.GetHandle("trackEvent:propertiesJson:"),
                    nsEvent.Handle,
                    nsJson.Handle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Amplitude iOS] Track failed: {ex}");
            }
        });
    }

    public void SetUserId(string userId)
    {
        if (_bridge is null) return;
        var handle = _bridge.Handle;
        Task.Run(() =>
        {
            try
            {
                using var ns = new NSString(userId);
                void_objc_msgSend_IntPtr(
                    handle, Selector.GetHandle("setUserId:"), ns.Handle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Amplitude iOS] SetUserId failed: {ex}");
            }
        });
    }

    public void Flush()
    {
        if (_bridge is null) return;
        var handle = _bridge.Handle;
        Task.Run(() =>
        {
            try
            {
                void_objc_msgSend(handle, Selector.GetHandle("flush"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Amplitude iOS] Flush failed: {ex}");
            }
        });
    }

    private bool ReadIsSessionReplayActive()
    {
        if (_bridge is null) return false;
        try
        {
            return bool_objc_msgSend(
                _bridge.Handle, Selector.GetHandle("isSessionReplayActive"));
        }
        catch
        {
            return false;
        }
    }
}
#endif
