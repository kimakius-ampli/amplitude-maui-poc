# AmplitudeIOSBindings

A thin Swift wrapper around [AmplitudeSwift](https://github.com/amplitude/Amplitude-Swift)
and [AmplitudeSessionReplay-iOS](https://github.com/amplitude/AmplitudeSessionReplay-iOS),
exposing `@objc`-visible APIs that the .NET MAUI app calls into via `ObjCRuntime`.

## Why

AmplitudeSwift is Swift-only: it uses generics, structs, and enums that don't
bridge to Objective-C. This wrapper adds an `@objc` class (`AMPMauiAmplitude`)
with a simple ABI (NSString in, JSON string for properties) that we can call
from C#.

## Build

```bash
cd NativeLibraries/AmplitudeIOSBindings
./build-xcframework.sh
```

This produces `build/AmplitudeIOSBindings.xcframework`. The MAUI csproj
references that artifact directly via `<NativeReference>` in the iOS
ItemGroup.

Re-run the script when bumping the SDK versions in `Package.swift`.

## ObjC contract (must match C# binding)

| ObjC selector                                              | Returns        |
|------------------------------------------------------------|----------------|
| `initializeWithApiKey:serverZone:sampleRate:`              | void           |
| `trackEvent:propertiesJson:`                               | void           |
| `setUserId:`                                               | void           |
| `flush`                                                    | void           |
| `isSessionReplayActive`                                    | BOOL           |

Class name (registered with the ObjC runtime): `AMPMauiAmplitude`.

If you rename anything in `AmplitudeBindings.swift`, update
`Platforms/iOS/AmplitudeIOSService.cs` in lockstep.
