#if ANDROID
using AmplitudeMauiPoc.Services;

namespace AmplitudeMauiPoc.Platforms.Android;

public class AmplitudeJniService : IAmplitudeService
{
    private Java.Lang.Object? _amplitudeInstance;
    private Java.Lang.Reflect.Method? _trackMethod;
    private Java.Lang.Reflect.Method? _setUserIdMethod;
    private Java.Lang.Reflect.Method? _flushMethod;
    private bool _sessionReplayActive;

    public bool IsSessionReplayActive => _sessionReplayActive;

    private Java.Lang.Object? CreateInstance(string className)
    {
        var cls = Java.Lang.Class.ForName(className);
        foreach (var ctor in cls.GetDeclaredConstructors()!)
        {
            var p = ctor.GetParameterTypes()!;
            if (p.Length == 0)
            {
                ctor.Accessible = true;
                return ctor.NewInstance();
            }
        }
        // Try synthetic no-arg (with DefaultConstructorMarker)
        foreach (var ctor in cls.GetDeclaredConstructors()!)
        {
            var p = ctor.GetParameterTypes()!;
            if (p.Length == 2 && p[p.Length - 1]!.Name == "kotlin.jvm.internal.DefaultConstructorMarker")
            {
                ctor.Accessible = true;
                return ctor.NewInstance(new Java.Lang.Integer(-1), null);
            }
        }
        // Last resort: largest synthetic constructor, all defaults
        Java.Lang.Reflect.Constructor? best = null;
        foreach (var ctor in cls.GetDeclaredConstructors()!)
        {
            if (best == null || ctor.GetParameterTypes()!.Length > best.GetParameterTypes()!.Length)
                best = ctor;
        }
        if (best != null)
        {
            var p = best.GetParameterTypes()!;
            var args = new Java.Lang.Object?[p.Length];
            for (int i = 0; i < p.Length; i++)
            {
                args[i] = p[i]!.Name switch
                {
                    "int" => new Java.Lang.Integer(-1),
                    "boolean" => new Java.Lang.Boolean(false),
                    _ => null
                };
            }
            best.Accessible = true;
            return best.NewInstance(args);
        }
        return null;
    }

    public void Init(string apiKey)
    {
        try
        {
            var context = global::Android.App.Application.Context;

            // Create Configuration
            var configClass = Java.Lang.Class.ForName("com.amplitude.android.Configuration");
            Java.Lang.Object? config = null;
            foreach (var ctor in configClass.GetDeclaredConstructors()!)
            {
                var p = ctor.GetParameterTypes()!;
                if (p.Length == 2 && p[0]!.Name == "java.lang.String" && p[1]!.Name == "android.content.Context")
                {
                    ctor.Accessible = true;
                    config = ctor.NewInstance(new Java.Lang.String(apiKey), context);
                    break;
                }
            }
            if (config == null) { global::Android.Util.Log.Error("AmpInit", "No config"); return; }

            // Create Amplitude
            var ampClass = Java.Lang.Class.ForName("com.amplitude.android.Amplitude");
            var ampCtor = ampClass.GetDeclaredConstructors()![0];
            ampCtor.Accessible = true;
            _amplitudeInstance = ampCtor.NewInstance(config);
            global::Android.Util.Log.Info("AmpInit", "Amplitude created");

            // Create InternalOptions
            try
            {
                var internalOpts = CreateInstance("com.amplitude.android.plugins.internal.InternalOptions");
                global::Android.Util.Log.Info("AmpInit", $"InternalOptions created: {internalOpts != null}");

                var srClass = Java.Lang.Class.ForName("com.amplitude.android.plugins.SessionReplayPlugin");
                foreach (var ctor in srClass.GetDeclaredConstructors()!)
                {
                    var p = ctor.GetParameterTypes()!;
                    if (p.Length == 9 && p[0]!.Name == "java.lang.Number")
                    {
                        // 9-param synthetic: Number, boolean, String, Integer, Integer,
                        //   InternalOptions, PrivacyConfig, int mask, DefaultConstructorMarker
                        ctor.Accessible = true;
                        var srPlugin = ctor.NewInstance(
                            new Java.Lang.Double(1.0),   // sampleRate
                            new Java.Lang.Boolean(true),  // enableRemoteConfig
                            null,                          // serverZone
                            null,                          // interactionCaptureMode
                            null,                          // privacyMode
                            internalOpts,                  // internalOptions (non-null!)
                            null,                          // privacyConfig
                            new Java.Lang.Integer(0b1100100), // defaults mask: bits for null params
                            null                           // DefaultConstructorMarker
                        );

                        var addMethod = ampClass.GetMethod("add",
                            Java.Lang.Class.ForName("com.amplitude.core.platform.Plugin"));
                        addMethod!.Invoke(_amplitudeInstance, srPlugin);
                        _sessionReplayActive = true;
                        global::Android.Util.Log.Info("AmpInit", "SR added with sampleRate=1.0!");
                        break;
                    }
                }

                if (!_sessionReplayActive)
                {
                    // Fallback: try 7-param with internalOpts
                    foreach (var ctor in srClass.GetDeclaredConstructors()!)
                    {
                        var p = ctor.GetParameterTypes()!;
                        if (p.Length == 7 && p[0]!.Name == "java.lang.Number")
                        {
                            ctor.Accessible = true;
                            var srPlugin = ctor.NewInstance(
                                new Java.Lang.Double(1.0),
                                new Java.Lang.Boolean(true),
                                null,
                                null,
                                null,
                                internalOpts,
                                null
                            );
                            var addMethod = ampClass.GetMethod("add",
                                Java.Lang.Class.ForName("com.amplitude.core.platform.Plugin"));
                            addMethod!.Invoke(_amplitudeInstance, srPlugin);
                            _sessionReplayActive = true;
                            global::Android.Util.Log.Info("AmpInit", "SR added via 7-param!");
                            break;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error("AmpInit", $"SR failed: {ex.InnerException?.Message ?? ex.Message}");
            }

            // Cache method handles so per-call reflection lookups don't run on the UI thread.
            try
            {
                _trackMethod = ampClass.GetMethod("track",
                    Java.Lang.Class.FromType(typeof(Java.Lang.String)),
                    Java.Lang.Class.ForName("java.util.Map"));
                _setUserIdMethod = ampClass.GetMethod("setUserId",
                    Java.Lang.Class.FromType(typeof(Java.Lang.String)));
                _flushMethod = ampClass.GetMethod("flush");
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error("AmpInit", $"Method cache failed: {ex.Message}");
            }

            global::Android.Util.Log.Info("AmpInit", $"Done. SR={_sessionReplayActive}");
        }
        catch (System.Exception ex)
        {
            global::Android.Util.Log.Error("AmpInit", $"Fatal: {ex}");
        }
    }

    public void Track(string eventName, Dictionary<string, object>? properties = null)
    {
        if (_amplitudeInstance is null || _trackMethod is null) return;

        // Snapshot props on the calling thread, run JNI on a worker so the UI thread
        // never waits on Java reflection or HashMap allocation.
        var propsCopy = properties is null ? null : new Dictionary<string, object>(properties);
        var instance = _amplitudeInstance;
        var method = _trackMethod;

        Task.Run(() =>
        {
            try
            {
                var map = new Java.Util.HashMap();
                if (propsCopy is not null)
                    foreach (var kvp in propsCopy)
                        map.Put(kvp.Key, kvp.Value?.ToString() ?? "");
                method.Invoke(instance, new Java.Lang.String(eventName), map);
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Warn("AmpTrack", ex.Message ?? "");
            }
        });
    }

    public void SetUserId(string userId)
    {
        if (_amplitudeInstance is null || _setUserIdMethod is null) return;
        var instance = _amplitudeInstance;
        var method = _setUserIdMethod;
        Task.Run(() =>
        {
            try { method.Invoke(instance, new Java.Lang.String(userId)); }
            catch { }
        });
    }

    public void Flush()
    {
        if (_amplitudeInstance is null || _flushMethod is null) return;
        var instance = _amplitudeInstance;
        var method = _flushMethod;
        Task.Run(() =>
        {
            try { method.Invoke(instance); }
            catch { }
        });
    }
}
#endif
