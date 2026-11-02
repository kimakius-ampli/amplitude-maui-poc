# Amplitude .NET MAUI PoC

A reference proof-of-concept showing Amplitude's native **Android (Kotlin)**
and **iOS (Swift)** SDKs — including **Session Replay** — running inside a
.NET MAUI app via native library interop.

> ## ⚠️ READ THIS FIRST
>
> - **This is a reference PoC, not a product.** It was put together by
>   Amplitude Solutions Engineering, with significant help from an AI
>   coding assistant, to show that the integration pattern works. Every
>   file in this repository — C#, Swift, MSBuild, shell scripts, and this
>   README — was written or edited with AI assistance and has had limited
>   human review.
> - **This code is NOT production-ready.** It has had no security audit,
>   no privacy review, no performance testing, and only ad-hoc QA on a
>   single Android emulator and a single iOS simulator. There are no
>   automated tests.
> - **The interop layer is not supported by Amplitude.** Amplitude
>   supports its native Android and iOS SDKs. The .NET MAUI interop layer
>   in this repository (the C# services, the JNI reflection code, and the
>   Swift `@objc` wrapper) is **owned by your engineering team**. Amplitude
>   will not triage issues, accept pull requests, or provide technical
>   support for it.
> - **Intended use is demo, proof, and prototyping only.** Treat the
>   contents as something to read, run, and learn from. Do not copy
>   patterns from here into a shipping product without independent
>   review by an engineer who understands .NET MAUI, JNI, and Objective-C
>   interop.

---

## What this sample shows

A single .NET MAUI app that calls the official Amplitude **Android Kotlin
SDK** and **iOS Swift SDK** — including Session Replay on both platforms —
from shared C# code, without hand-writing C# bindings for either SDK.

Shared MAUI UI code programs against `IAmplitudeService`
(`Services/IAmplitudeService.cs`). Each platform provides its own
implementation under `Platforms/`.

### What it does NOT show
- Any production-grade key management — the API key is read from a local
  `.env` file or environment variable.
- Privacy masking configuration for Session Replay — sample rate is 1.0
  (100% capture) and no PII masking is configured.
- iOS App Transport Security pinning, Android network security config,
  or any other transport hardening beyond platform defaults.
- Background event flushing, retry/backoff, offline queueing — relies
  entirely on the SDKs' defaults.
- Tests of any kind.

---

## Standard implementation pattern

This repository is a worked example of the **Native Library Interop**
pattern published by the [.NET MAUI Community Toolkit][nli]:

> "Native Library Interop is a sample pattern for using native iOS and
> Android libraries in a .NET MAUI app."

There are two halves to that pattern, one per platform, and this repo
implements both:

| Platform | Mechanism this repo uses | Microsoft Learn references |
|---|---|---|
| Android | `<AndroidMavenLibrary ... Bind="false" />` pulls the Amplitude Kotlin SDK from Maven Central into the APK. The app then calls into the Kotlin classes at runtime via JNI reflection (`Java.Lang.Class.ForName` + `GetDeclaredConstructors`). | [Binding a Java library from Maven][android-maven] · [Binding Java libraries overview (JNI bridge / MCW)][android-jni] |
| iOS | A Swift wrapper package (`NativeLibraries/AmplitudeIOSBindings`) annotated with `@objc` / `@objcMembers`, built into an `.xcframework`, and consumed via `<NativeReference>` in the MAUI csproj. C# calls the wrapper's selectors with `objc_msgSend`. | [Native references for iOS / Mac][ios-native-ref] · [Type registrar (`[Register]` / `[Export]`)][ios-registrar] · [Binding Objective-C libraries][ios-objc-bind] |

Note: at the time of writing, some of the iOS interop pages still live in
the archived Xamarin section of Microsoft Learn — they have not yet been
migrated into the `/dotnet/ios/` namespace. They are linked from
learn.microsoft.com and remain the canonical Microsoft references for the
underlying mechanisms (`<NativeReference>`, `[Register]`, `[Export]`).

The Amplitude SDKs this sample calls into:

- Android (Kotlin): <https://github.com/amplitude/Amplitude-Kotlin>
- iOS (Swift): <https://github.com/amplitude/Amplitude-Swift>
- iOS Session Replay: <https://github.com/amplitude/AmplitudeSessionReplay-iOS>

.NET MAUI / .NET for Android / .NET for iOS documentation roots:

- .NET MAUI: <https://learn.microsoft.com/en-us/dotnet/maui/>
- .NET for Android: <https://learn.microsoft.com/en-us/dotnet/android/>
- .NET for iOS: <https://learn.microsoft.com/en-us/dotnet/ios/>

[nli]: https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/native-library-interop/
[android-maven]: https://learn.microsoft.com/en-us/dotnet/android/binding-libs/binding-java-libs/binding-java-maven-library
[android-jni]: https://learn.microsoft.com/en-us/dotnet/android/binding-libs/binding-java-libs/
[ios-native-ref]: https://learn.microsoft.com/en-us/previous-versions/xamarin/cross-platform/macios/native-references
[ios-registrar]: https://learn.microsoft.com/en-us/previous-versions/xamarin/ios/internals/registrar
[ios-objc-bind]: https://learn.microsoft.com/en-us/previous-versions/xamarin/cross-platform/macios/binding/objective-c-libraries

---

## Architecture

```
   Shared MAUI UI  (Views/, Models/, MauiProgram.cs)
              |
              v
   IAmplitudeService  (Services/IAmplitudeService.cs)
       /          \
      /            \
 Android           iOS
   |                |
 AmplitudeJniService.cs        AmplitudeIOSService.cs
   |                |
 JNI reflection    objc_msgSend on selectors
   |                |
   v                v
 Amplitude AARs    AmplitudeIOSBindings.xcframework
 (Maven Central)   (Swift wrapper, built locally)
                       |
                       v
                AmplitudeSwift + AmplitudeSessionReplay
                (Swift Package Manager, fetched at build)
```

---

## Prerequisites

- **.NET 10 SDK** (`dotnet --version` should show `10.x`)
- **MAUI workload** (`sudo dotnet workload install maui`)
- **Android**: Android Studio (or `sdkmanager`) with SDK 34+, an
  emulator, and `ANDROID_HOME` set. Project targets
  `SupportedOSPlatformVersion = 24.0` (Android 7.0).
- **iOS** (optional): macOS with Xcode 15+. Project targets
  `SupportedOSPlatformVersion = 16.0` (iOS 16).

The Android target uses `<AndroidMavenLibrary>`, which requires .NET 9
or later. This project targets `net10.0-android` and `net10.0-ios`.

---

## API key setup

The app reads the Amplitude API key in this order:

1. `AMPLITUDE_API_KEY` environment variable (useful for CI / local shell)
2. `.env` file at the project root (gitignored, bundled into the app as
   a MAUI asset under logical name `env`)
3. Falls back to a placeholder; Amplitude won't initialize.

To set up locally:

```bash
cp .env.example .env
# edit .env and replace YOUR_AMPLITUDE_API_KEY_HERE with your real key
```

`.env` is gitignored; `.env.example` is the committed template. The file
uses standard `KEY=VALUE` syntax (one per line, `#` comments allowed,
optional `export` prefix allowed).

---

## Android — build & run

```bash
export JAVA_HOME="/Applications/Android Studio.app/Contents/jbr/Contents/Home"
dotnet restore
dotnet build -t:Run -f net10.0-android
```

The first build takes a few minutes — MSBuild downloads the Amplitude
AARs from Maven Central, packages them into the APK, and compiles the
C# host code.

`Platforms/Android/AmplitudeJniService.cs` uses JNI reflection to
construct the Kotlin types (`Configuration`, `Amplitude`,
`SessionReplayPlugin`) and add the Session Replay plugin to the
Amplitude instance. Session Replay sample rate is hard-coded to `1.0`.

> **Recommended production step (Android):** JNI reflection was chosen
> here because it needs no binding project and proves the SDK runs inside
> MAUI with minimal setup. It is, however, brittle: it looks up Kotlin
> constructors and methods by name and parameter count at runtime, so an
> SDK upgrade that changes a signature fails at runtime rather than at
> compile time, and every property value is passed as a string. For
> production, replace it with a **thin, typed Kotlin wrapper** — a small
> Android library exposing a flat, Java-friendly API (`init`, `track`,
> `identify`, `setUserId`, `reset`, `flush`, …) that is bound to C# via a
> standard Android binding. This mirrors what the Swift `@objc` wrapper
> already does on iOS.

The list of Amplitude AARs pulled from Maven Central and their pinned
versions is in `AmplitudeMauiPoc.csproj` under the Android `ItemGroup`.

---

## iOS — build & run

### One-time: build the Swift wrapper xcframework

```bash
cd NativeLibraries/AmplitudeIOSBindings
./build-xcframework.sh
cd ../..
```

This produces:

```
NativeLibraries/AmplitudeIOSBindings/build/
    AmplitudeIOSBindings.xcframework        # our @objc wrapper
    AmplitudeCore.xcframework               # transitive, shipped by Amplitude
    AmplitudeSessionReplay.xcframework      # transitive, shipped by Amplitude
```

Re-run the script when bumping SDK versions in
`NativeLibraries/AmplitudeIOSBindings/Package.swift`.

The csproj references the wrapper xcframework via `<NativeReference>`
and uses a post-build MSBuild target to copy the two transitive
xcframeworks into the `.app/Frameworks/` directory (the C# toolchain
does not embed transitive xcframeworks automatically).

### Build & run

```bash
dotnet build -t:Run -f net10.0-ios -p:_DeviceName=:v2:udid=<simulator-udid>
```

Or, to compile only:

```bash
dotnet build -f net10.0-ios
```

`Platforms/iOS/AmplitudeIOSService.cs` instantiates the
`AMPMauiAmplitude` ObjC class from the xcframework and calls its
selectors via `objc_msgSend`. Server zone is `"US"` (the default for
the Amplitude SDK), sample rate is `1.0`.

---

## Key files

| Path | Purpose |
|---|---|
| `Services/IAmplitudeService.cs` | Cross-platform interface (`Init`, `Track`, `SetUserId`, `Flush`, `IsSessionReplayActive`) |
| `Platforms/Android/AmplitudeJniService.cs` | Android implementation (JNI reflection) |
| `Platforms/iOS/AmplitudeIOSService.cs` | iOS implementation (`objc_msgSend` selectors) |
| `NativeLibraries/AmplitudeIOSBindings/` | Swift `@objc` wrapper package + build script |
| `MauiProgram.cs` | DI wiring + Amplitude init + `.env` loader |
| `AmplitudeMauiPoc.csproj` | `AndroidMavenLibrary` (Android) + `NativeReference` (iOS) |
| `Views/HomePage.xaml(.cs)` | Home screen: item search + sample item list |
| `Views/ItemDetailPage.xaml(.cs)` | Item detail screen with CTAs |
| `Models/DemoItem.cs` | Sample data model |

---

## Demo events

The sample screens send a small, generic set of events so you can see data
and Session Replay end-to-end in Amplitude. Event names use Title Case;
property names use snake_case.

| Event | Fired when | Properties |
|---|---|---|
| `Screen Viewed` | Home or Item Detail screen opens | `screen_name`; on Item Detail also `item_category`, `item_status`, `activity_count` |
| `Item Searched` | Search button tapped with a non-empty code | `query_length`, `input_method`, `result_found` |
| `Item Tapped` | An item in the Home list is tapped | `item_category`, `item_status` |
| `Button Tapped` | "Add to my list" or "More options" tapped | `button_name`, `screen_name`, `time_on_page_sec` |
| `Item Added` | "Add to my list" confirmed | `item_category` |
| `Option Selected` | An option chosen from "More options" | `option`, `item_category` |
| `Screen Closed` | Item Detail screen closes | `screen_name`, `time_on_page_sec` |

Sample item codes are `DEMO-1001`, `DEMO-1002` and `DEMO-1003`.

---

## Things deliberately left as PoC defaults

If you adapt any of this for real use, these are the parts that need
work first:

- **Session Replay sample rate**: `1.0` (100% capture) on both platforms.
  For real traffic, start much lower (e.g. `0.01`) and tune by volume.
- **Privacy masking**: Session Replay is initialized with the SDK
  defaults. Review masking config against your data-protection
  obligations before pointing this at anything real — any personal
  data your real screens render (names, account identifiers, free-text
  input) would be captured.
- **API key handling**: a local `.env` is fine for a demo. Production
  builds should pull from CI secrets or a remote configuration service,
  and the app should not ship the key in the bundle.
- **Transport hardening**: iOS uses default ATS (HTTPS + CA-signed);
  Android uses platform defaults. There is no certificate or public-key
  pinning.
- **Error handling**: most failure paths are logged-and-swallowed so
  the demo keeps running. Production code should fail loudly.

---

## Troubleshooting

### Android: `AndroidMavenLibrary is not supported`
You need .NET 9 or later for `<AndroidMavenLibrary>`. This project targets
`net10.0-android`. Check `dotnet --version`.

### Android: binding compilation errors
The Kotlin SDK is included with `Bind="false"`, so the toolchain should
not be generating C# bindings for it. If you see binding errors, confirm
each `AndroidMavenLibrary` entry in the csproj still has `Bind="false"`.

### iOS: `AMPMauiAmplitude not found` at runtime
The xcframework didn't link in. Confirm:
1. `NativeLibraries/AmplitudeIOSBindings/build/AmplitudeIOSBindings.xcframework`
   exists (run `build-xcframework.sh` if not).
2. `dotnet build` for iOS picked up the `<NativeReference>` entries.
   Re-run a clean build: `dotnet clean && dotnet build -f net10.0-ios`.

### iOS: build script fails to archive
Make sure Xcode 15+ is installed and `xcode-select -p` points at the
full Xcode (not just Command Line Tools). Open Xcode once to accept
the EULA.

### Amplitude shows no Session Replay
- Confirm the API key matches the target Amplitude project.
- Session Replay needs a short delay after app close to upload.
- Android: `adb logcat | grep -i amplitude` for diagnostics.
- iOS: filter `[Amplitude iOS]` in the Xcode console.
