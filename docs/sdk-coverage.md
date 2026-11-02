# Amplitude SDK Coverage Report

**Repo:** amplitude-maui-poc (.NET MAUI proof-of-concept)
**Scope:** What of the native Amplitude Android (Kotlin) and iOS (Swift) SDK surface is currently reachable from shared C# code.
**Status:** Proof-of-concept. Not production-ready. See [`README.md`](../README.md) for full context.

---

## Executive summary (for EMs / PMs)

This PoC demonstrates that a .NET MAUI app **can** call directly into the native Amplitude SDKs (Android Kotlin + iOS Swift), including Session Replay, without hand-writing C# bindings. The integration pattern works end-to-end — events flow, users get identified, Session Replay records.

**The shared C# interface today covers the core analytics calls a typical mobile app makes** — initialize, track named events with properties, set a user ID, flush, and install Session Replay. That's enough to ship a basic analytics integration.

**What's missing is the set of features most production apps add as they mature**: the `Identify` API (user properties), `reset()` on logout, `optOut` for consent flows, `serverZone` selection for EU residency, Session Replay sample-rate and privacy-mask configuration, and revenue tracking. None of these are *exotic* SDK features — they're standard for production rollouts, and they are not blockers for the PoC itself.

**A small remainder is rarely used by typical customers** (B2B group analytics, custom plugin authoring, network-tracking plugin, COPPA mode, multiple SDK instances). Most teams will never need these.

Headline figures, framed by how commonly each SDK feature is adopted in real implementations:

| Adoption tier | What it includes | PoC coverage |
|---|---|---|
| **T1 — Core (almost everyone)** | Init, basic `track`, `setUserId`, `flush`, Session Replay install | ✅ Covered (with some hardcoded values) |
| **T2 — Standard production add-ons** | Identify API, `reset`, `optOut`, `serverZone` choice, SR sample-rate + privacy config | ❌ Missing — would block a production launch |
| **T3 — Common in specific contexts** | `revenue` (monetized apps), `setGroup` (B2B), `setDeviceId`, autocapture tuning, custom logging | ❌ Missing — add only if the use case applies |
| **T4 — Rarely needed** | `groupIdentify`, custom plugins, `NetworkTrackingPlugin`, `setSessionId`, COPPA, multi-instance | ❌ Missing — most teams will never implement these |

Expanding from T1 to T1+T2 is the recommended next step. It's mechanical work — each missing method follows the same interop pattern as the existing ones — but Session Replay privacy masking and consent management touch privacy/legal and need review before any pilot.

---

## How this report is organized

For each platform we list every public SDK feature grouped by category, and mark coverage as:

- ✅ — fully exposed through `IAmplitudeService`
- ✅ Partial — exposed but with limitations (lossy types, hardcoded values, etc.)
- ✅ Inherited — works only because the native SDK does it automatically; not controllable from C#
- ❓ — SDK provides this by default but is not validated in a MAUI context
- ❌ — not reachable from C# without changes to the binding layer

Each feature also carries an **adoption tier** marker (**T1**–**T4**) showing how commonly it's implemented by Amplitude customers. The tier framework lets you distinguish *"missing and most customers would want it"* from *"missing and most customers will never miss it"*.

| Tier | Meaning | Typical for |
|---|---|---|
| **T1** | Almost every implementation uses this | Any app sending events to Amplitude |
| **T2** | Most production apps add this within the first few months | Apps doing consent, logout, user properties, EU residency |
| **T3** | Common only in specific contexts | Monetized apps, B2B apps, debug-heavy teams |
| **T4** | Rarely implemented by typical customers | Advanced enrichment, niche compliance, multi-tenancy edge cases |

Tier classifications are based on Amplitude SE experience, not formal customer telemetry, and should be treated as guidance rather than fact.

Coverage is measured **as of the initial release of this repository**. Re-run this audit after the binding surface is expanded.

---

## Integration layer (where the bindings live)

| File | Role |
|---|---|
| [`Services/IAmplitudeService.cs`](../Services/IAmplitudeService.cs) | Cross-platform C# interface the rest of the MAUI app programs against. The *entire* surface area exposed to shared code: `Init`, `Track`, `SetUserId`, `Flush`, `IsSessionReplayActive`. |
| [`Platforms/Android/AmplitudeJniService.cs`](../Platforms/Android/AmplitudeJniService.cs) | Android implementation. Uses JNI reflection (`Java.Lang.Class.ForName` + `GetDeclaredConstructors`) to instantiate Kotlin SDK classes from the AARs pulled via `<AndroidMavenLibrary Bind="false">`. |
| [`Platforms/iOS/AmplitudeIOSService.cs`](../Platforms/iOS/AmplitudeIOSService.cs) | iOS implementation. Uses `objc_msgSend` P/Invokes against the Swift wrapper's ObjC selectors. |
| [`NativeLibraries/AmplitudeIOSBindings/Sources/AmplitudeIOSBindings/AmplitudeBindings.swift`](../NativeLibraries/AmplitudeIOSBindings/Sources/AmplitudeIOSBindings/AmplitudeBindings.swift) | Slim `@objc` Swift wrapper around `AmplitudeSwift` + Session Replay. Required because Swift symbols aren't directly callable from C# — every iOS feature we want to expose to C# needs a selector here first. |
| [`AmplitudeMauiPoc.csproj`](../AmplitudeMauiPoc.csproj) | Declares which native artifacts ship inside the app (Maven AARs for Android, xcframeworks for iOS). |

> **Key implication for the dev team:** adding any new iOS SDK feature to C# requires a **two-step** change — first expose it in `AmplitudeBindings.swift` and rebuild the xcframework, then call its selector from `AmplitudeIOSService.cs`. Adding an Android feature is **one step** — extend `AmplitudeJniService.cs` to call the Kotlin method via JNI reflection. Both platforms also need a corresponding method added to `IAmplitudeService.cs`.

---

## Android (Amplitude Kotlin SDK) — gap analysis

Reference: <https://amplitude.com/docs/sdks/analytics/android/android-kotlin-sdk>
Source verified against: <https://github.com/amplitude/Amplitude-Kotlin>

### Initialization & Configuration

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `Amplitude(apiKey, context, Configuration)` constructor | T1 | ✅ Partial | Only `apiKey` flows through C#. No `Configuration` object is built or passed. |
| `flushIntervalMillis` | T3 | ❌ | SDK default (30 000 ms). |
| `flushQueueSize` | T3 | ❌ | SDK default (30). |
| `serverUrl` (custom endpoint) | T4 | ❌ | Rare; for proxies / self-host. |
| `serverZone` (US / EU) | **T2** | ❌ | Android defaults to US — cannot select EU residency from C#. Needed for any deployment with EU data subjects. |
| `useBatch` | T3 | ❌ | High-volume mode. |
| `httpClient` (custom `HttpClientInterface`) | T4 | ❌ | Custom networking integrations. |
| `minTimeBetweenSessionsMillis` | T3 | ❌ | Default 30 min. |
| `enableCoppaControl` | T4 | ❌ | Only relevant to children's apps. |
| `useAdvertisingIdForDeviceId` (GAID) | T3 | ❌ | Common in ad-attributed apps. |
| `useAppSetIdForDeviceId` | T3 | ❌ | Privacy-friendly GAID alternative. |
| `loggerProvider` / `Logger.LogMode` | T3 | ❌ | App logs via `android.util.Log` instead. |
| `identifyBatchIntervalMillis` | T4 | ❌ | Advanced batching tuning. |
| `minIdLength` | T4 | ❌ | |
| `partnerId` / `plan` / `ingestionMetadata` | T4 | ❌ | Partner integrations / tracking plan metadata. |
| `enableDiagnostics` / `enableRequestBodyCompression` | T4 | ❌ | |
| `instanceName` / multiple SDK instances | T4 | ❌ | Multi-tenant edge case. |
| Offline queueing | T1 | ✅ Inherited | SDK default. |

### Event Tracking

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `track(eventType: String)` | T1 | ✅ | `Track(name)` overload (props=null). |
| `track(eventType, Map<String, Any?>)` | T1 | ✅ Partial | All property values are coerced to `String` in `AmplitudeJniService.Track`. Numbers, booleans, arrays, nested maps are stringified — **lossy**. Fixing this means building a real `Java.Util.HashMap` with typed values. |
| `track(BaseEvent, EventOptions, EventCallback)` | T4 | ❌ | Most teams use the string overload. |

### User Identity

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `setUserId(userId: String?)` | T1 | ✅ | |
| `getUserId(): String?` | T3 | ❌ | Useful for debug / cross-system correlation. |
| `setDeviceId(deviceId: String)` | T3 | ❌ | Most apps let the SDK auto-generate. |
| `getDeviceId(): String?` | T3 | ❌ | |
| `sessionId: Long` (property) | T3 | ❌ | Note: exposed as a *property* on Android (no `getSessionId()` method). |
| `reset()` (anonymize on logout) | **T2** | ❌ | Required for any app with logout/account-switching. |

### User Properties (Identify API)

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `identify(Identify, EventOptions?)` | **T2** | ❌ | **Entire Identify API is missing.** Production apps rely on user properties for cohorts, segmentation, behavioural analysis. |
| `Identify.set(key, value)` | **T2** | ❌ | Most common operator. |
| `Identify.setOnce(key, value)` | **T2** | ❌ | E.g. acquisition source, first-seen timestamp. |
| `Identify.add(key, Number)` | **T2** | ❌ | Increment counters. |
| `Identify.append(key, value)` | T3 | ❌ | |
| `Identify.unset(key)` | T3 | ❌ | |
| `Identify.clearAll()` | T3 | ❌ | |
| `Identify.prepend / preInsert / postInsert / remove` | T4 | ❌ | Edge cases. |

### Group Analytics

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `setGroup(groupType, groupName, EventOptions?)` | T3 | ❌ | Standard for B2B apps. |
| `setGroup(groupType, Array<String>, EventOptions?)` | T3 | ❌ | |
| `groupIdentify(groupType, groupName, Identify)` | T4 | ❌ | Niche B2B use case. |

### Revenue Tracking

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `revenue(Revenue, EventOptions?)` / `revenue(RevenueEvent)` | T3 | ❌ | Needed only if the app monetizes (e.g. in-app purchases). |
| Revenue fields (`productId`, `quantity`, `price`, `revenueType`, `receipt`, `receiptSignature`) | T3 | ❌ | |

### Session Management

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| Automatic session events | T1 | ✅ Inherited | SDK default. |
| `sessionId` (read) | T3 | ❌ | |
| Out-of-session events (`EventOptions { sessionId = -1 }`) | T4 | ❌ | |
| `minTimeBetweenSessionsMillis` tuning | T3 | ❌ | |

### Flushing & Lifecycle

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `flush()` | T1 | ✅ | Dispatched on `Task.Run` to avoid blocking UI. |
| Periodic flush tuning | T3 | ❌ | Uses defaults. |

### Plugins & Middleware

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `add(plugin: Plugin)` from C# | T4 | ❌ | Used internally once to install Session Replay. C#-authored plugins are an advanced extension. |
| `remove(plugin: Plugin)` | T4 | ❌ | |
| `Plugin.setup` / `Plugin.execute` (enrichment) | T4 | ❌ | |
| `DestinationPlugin` (custom destinations) | T4 | ❌ | |
| `NetworkTrackingPlugin` (OkHttp HTTP capture) | T4 | ❌ | |

### Session Replay

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `SessionReplayPlugin` installed | T1 | ✅ | Via JNI reflection in `AmplitudeJniService.Init`. |
| `IsSessionReplayActive` flag | T1 | ✅ | |
| Configurable `sampleRate` | **T2** | ❌ | **Hardcoded `1.0` (100% capture)** — must be tuned before production. |
| Privacy / masking config (`privacyConfig`, `privacyMode`) | **T2** | ❌ | **No PII masking configured.** Any personal data rendered on screen (names, account IDs, free-text input) would be captured. |
| `serverZone` for SR | T3 | ❌ | Hardcoded null → SDK default. |
| `interactionCaptureMode` | T4 | ❌ | |

### Autocapture / Default Tracking

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `SESSIONS` | T1 | ✅ Inherited | SDK default. |
| `APP_LIFECYCLES` | T1 | ✅ Inherited | SDK default. |
| `SCREEN_VIEWS` | T1 | ✅ Inherited | SDK default — but MAUI uses its own page navigation, not native Activity/Fragment, so coverage is partial in practice. |
| `DEEP_LINKS` | T3 | ❓ | Not validated in this PoC. |
| `ELEMENT_INTERACTIONS` | T3 | ❓ | Native Android views; MAUI's rendering may not surface usefully. |
| `FRUSTRATION_INTERACTIONS` | T3 | ❓ | Same caveat. |
| `autocaptureOptions { }` configuration from C# | T3 | ❌ | Cannot tune which autocapture options run. |

### Privacy / Opt-Out

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `optOut` (Configuration property + runtime setter) | **T2** | ❌ | **Production blocker for GDPR/CCPA consent flows.** |
| `enableCoppaControl` | T4 | ❌ | Children's apps only. |
| `TrackingOptions.disableIpAddress` | **T2** | ❌ | Commonly disabled in EU deployments. |
| `TrackingOptions.disable*` (carrier, city, country, DMA, language, lat/lng, region, device fields, OS fields, version, API level, ADID, AppSetId) | T3 | ❌ | Privacy-sensitive deployments selectively disable. |

### Other

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| Deep link `setIntent(intent)` for singleTask/singleTop activities | T3 | ❌ | Required only if app has deep-link entry points and singleTask launch mode. |
| Event callbacks (post-transmission status, via `Configuration.callback`) | T4 | ❌ | |

---

## iOS (Amplitude Swift SDK) — gap analysis

Reference: <https://amplitude.com/docs/sdks/analytics/ios/ios-swift-sdk>
Source verified against: <https://github.com/amplitude/Amplitude-Swift>

### Initialization & Configuration

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `Amplitude(configuration: Configuration)` | T1 | ✅ Partial | Only `apiKey` + `serverZone` flow through. |
| `apiKey` | T1 | ✅ | |
| `serverZone` | **T2** | ✅ Partial | Selector accepts it, but C# (`AmplitudeIOSService.cs:21`) hardcodes `"US"`. To switch to EU, edit the constant. |
| `autocapture` (AutocaptureOptions) | T3 | ❌ | SDK defaults apply. |
| `enableAutoCaptureRemoteConfig` | T4 | ❌ | |
| `networkTrackingOptions` | T4 | ❌ | |
| `interactionsOptions` (rage/dead click tuning) | T4 | ❌ | |
| `minTimeBetweenSessionsMillis` | T3 | ❌ | |
| `logLevel` | T3 | ❌ | |
| `trackingOptions` | T3 | ❌ | |
| `storageProvider` | T4 | ❌ | |
| `enableCoppaControl` | T4 | ❌ | |
| `optOut` (Configuration property + runtime setter) | **T2** | ❌ | |
| `instanceName` / multiple instances | T4 | ❌ | |
| `loggerProvider` | T3 | ❌ | App logs via `Debug.WriteLine`. |
| `offline` (`NetworkConnectivityCheckerPlugin`) | T4 | ❌ | |
| `useBatch` | T3 | ❌ | |

### Event Tracking

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `track(eventType, eventProperties)` | T1 | ✅ | Properties JSON-serialized in C# and re-parsed inside the Swift wrapper. Types survive round-trip better than Android's stringification, but anything outside `[String: Any]`-compatible JSON is lossy. |
| `track(event: BaseEvent, options:, callback:)` | T4 | ❌ | |
| `BaseEvent` props (`groups`, `sessionId`, etc.) | T4 | ❌ | |
| `ScreenViewedEvent(screenName:)` | T3 | ❌ | Most teams just call `track`. |
| `DeepLinkOpenedEvent(url:[referrer:])` / `(activity:)` | T3 | ❌ | |
| `EventOptions` (per-event overrides) | T4 | ❌ | |

### User Identity

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `setUserId(userId: String?)` | T1 | ✅ | |
| `getUserId() -> String?` | T3 | ❌ | |
| `setDeviceId(deviceId: String?)` | T3 | ❌ | |
| `getDeviceId() -> String?` | T3 | ❌ | |
| `getSessionId() -> Int64` | T3 | ❌ | |
| `setSessionId(timestamp:)` / `setSessionId(date:)` | T4 | ❌ | iOS-only; manual session control. |
| `reset()` (anonymize on logout) | **T2** | ❌ | Required for any app with logout. |

### User Properties (Identify API)

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `identify(Identify, options:)` | **T2** | ❌ | **Entire Identify API missing.** |
| `identify(userProperties: [String: Any], options:)` | **T2** | ❌ | Convenience overload. |
| `Identify.set(property, value)` | **T2** | ❌ | |
| `Identify.setOnce(property, value)` | **T2** | ❌ | |
| `Identify.add(property, NSNumber)` | **T2** | ❌ | |
| `Identify.unset(property)` | T3 | ❌ | |
| `Identify.append(property, value)` | T3 | ❌ | |
| `Identify.clearAll()` | T3 | ❌ | |
| `Identify.prepend / preInsert / postInsert / remove` | T4 | ❌ | |

### Group Analytics

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `setGroup(groupType, groupName, options:)` | T3 | ❌ | B2B / multi-tenant. |
| `setGroup(groupType, [String], options:)` | T3 | ❌ | |
| `groupIdentify(groupType, groupName, identify:, options:)` | T4 | ❌ | |
| `groupIdentify(groupType, groupName, groupProperties:, options:)` | T4 | ❌ | |

### Revenue Tracking

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `revenue(Revenue, options:)` | T3 | ❌ | |
| Revenue fields (`productId`, `quantity`, `price`, `revenueType`, `receipt`, `receiptSignature`) | T3 | ❌ | |

### Session Management

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| Automatic sessions | T1 | ✅ Inherited | SDK default. |
| `getSessionId()` / `sessionId` property | T3 | ❌ | |
| `setSessionId(...)` | T4 | ❌ | Manual session control. |
| `EventOptions(sessionId:)` out-of-session | T4 | ❌ | |
| `minTimeBetweenSessionsMillis` tuning | T3 | ❌ | |

### Flushing & Lifecycle

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `flush()` | T1 | ✅ | Dispatched on `Task.Run`. |

### Plugins & Middleware

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `add(plugin: UniversalPlugin)` from C# | T4 | ❌ | Used internally once in the Swift wrapper to install Session Replay. |
| `remove(plugin: UniversalPlugin)` | T4 | ❌ | |
| `Plugin` protocol authoring | T4 | ❌ | Requires extending the Swift wrapper. |
| `DestinationPlugin` override points | T4 | ❌ | |

### Session Replay

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `AmplitudeSwiftSessionReplayPlugin` installed | T1 | ✅ | Added inside the Swift wrapper. |
| `IsSessionReplayActive` flag | T1 | ✅ Partial | The Swift wrapper sets the flag unconditionally after `add(plugin:)` — it does not verify the plugin actually started. Treat as optimistic, not a health check. |
| Configurable `sampleRate` | **T2** | ✅ Partial | Selector accepts it, but C# hardcodes `1.0`. |
| Privacy masking config | **T2** | ❌ | Plugin constructed with `sampleRate` only — no masking options passed. |
| Server zone / data residency for SR | T3 | ❌ | |

### Autocapture / Default Tracking

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `sessions` | T1 | ✅ Inherited | SDK default. |
| `appLifecycles` | T1 | ✅ Inherited | SDK default. |
| `screenViews` | T1 | ✅ Inherited | SDK default — same MAUI navigation caveat as Android. |
| `networkTracking` | T3 | ❓ | SDK default; not validated. |
| `elementInteractions` | T3 | ❓ | MAUI-rendered UIViews; not validated. |
| `frustrationInteractions` | T3 | ❓ | Same caveat. |
| `AutocaptureOptions` configurable from C# | T3 | ❌ | |

### Privacy / Opt-Out

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `optOut` (Configuration + runtime setter) | **T2** | ❌ | **Production blocker for consent flows.** |
| `enableCoppaControl` | T4 | ❌ | |
| `TrackingOptions.disableTrackIpAddress` | **T2** | ❌ | Commonly disabled in EU deployments. |
| `TrackingOptions.disableTrack*` (Carrier, City, Country, DeviceModel, DeviceManufacturer, DMA, Language, IDFV, OsName, OsVersion, Platform, Region, VersionName) | T3 | ❌ | |

### Logging

| Feature | Tier | Covered | Notes |
|---|---|---|---|
| `logLevel` (OFF / ERROR / WARN / LOG / DEBUG) | T3 | ❌ | |
| Custom `Logger` protocol | T3 | ❌ | |

---

## Cross-platform summary

| Category | Tier | Android | iOS |
|---|---|---|---|
| Event tracking (basic) | T1 | ✅ (lossy types) | ✅ (JSON round-trip) |
| Init with API key | T1 | ✅ | ✅ |
| `setUserId` | T1 | ✅ | ✅ |
| `flush` | T1 | ✅ | ✅ |
| Session Replay install | T1 | ✅ (hardcoded sampleRate 1.0, no masking) | ✅ (hardcoded sampleRate 1.0, no masking) |
| Default autocapture | T1 | ✅ inherited (MAUI caveat) | ✅ inherited (MAUI caveat) |
| `reset` on logout | **T2** | ❌ | ❌ |
| Identify API (`set`, `setOnce`, `add`) | **T2** | ❌ | ❌ |
| `optOut` (consent) | **T2** | ❌ | ❌ |
| `serverZone` selection | **T2** | ❌ | ❌ partial (hardcoded `"US"`) |
| SR sample-rate configurable | **T2** | ❌ | ❌ (hardcoded `1.0`) |
| SR privacy masking | **T2** | ❌ | ❌ |
| `TrackingOptions.disableIpAddress` | **T2** | ❌ | ❌ |
| Revenue | T3 | ❌ | ❌ |
| `setGroup` (B2B) | T3 | ❌ | ❌ |
| `deviceId` get/set | T3 | ❌ | ❌ |
| Autocapture configuration | T3 | ❌ | ❌ |
| Custom logging | T3 | ❌ | ❌ |
| Advanced event types (`BaseEvent`, `EventOptions`, specialized events) | T4 | ❌ | ❌ |
| C#-authored plugins | T4 | ❌ | ❌ |
| `groupIdentify` | T4 | ❌ | ❌ |
| Multiple instances | T4 | ❌ | ❌ |
| `setSessionId` / out-of-session events | T4 | ❌ | ❌ |
| COPPA | T4 | ❌ | ❌ |

**Reading this table:** T1 is essentially covered. T2 is where the production gap sits. T3 is feature-specific (only add what the app actually needs). T4 is mostly safe to ignore.

---

## Recommended next steps before production adoption

Prioritized by adoption tier and by privacy/legal risk.

**Must do before any pilot with real users (T2 + privacy):**

1. **Privacy & consent (legal blocker).** Expose `optOut` on `IAmplitudeService`, wire it to the app's consent banner, and configure Session Replay privacy masking. Until this is done, do not point the app at real user traffic.
2. **Session Replay sample rate.** Move `1.0` out of the hardcoded constant and expose it as a config parameter. Pick a real production rate (typically `0.01`–`0.1`).
3. **Server zone selection.** If any data subjects are in the EU, the SDK must be initialized with the EU zone. Currently hardcoded to US on iOS and unset (defaulting US) on Android.
4. **Identify API.** Add `Identify` + the common operators (`set`, `setOnce`, `add`) to `IAmplitudeService` and to both native implementations. User properties power cohorts, segmentation, and most production analyses.
5. **`reset()` on logout.** One-line interface addition, but skipping it means logged-out users keep the prior user's ID until app reinstall.
6. **`TrackingOptions.disableIpAddress`** if the app has EU users.

**Add as needed by use case (T3):**

7. **Revenue tracking** — only if the app monetizes (e.g. in-app purchases).
8. **`setGroup`** — only if the business needs account/org-level analytics (B2B view).
9. **`setDeviceId` / `getDeviceId` / `getUserId`** — for cross-system identity correlation or debugging.
10. **Autocapture configuration** — once the team decides which automatic events they want enabled in the production build.
11. **Custom logger / `logLevel`** — to route SDK logs through the app's existing logging pipeline.
12. **Typed event properties on Android.** Replace the `String`-coercion in `AmplitudeJniService.Track` with a real `Java.Util.HashMap` build that preserves numbers, booleans, and nested structures. (Currently the C# layer accepts `Dictionary<string, object>` but Android flattens everything to strings; iOS preserves types via JSON round-trip.)

**Defer unless a specific need surfaces (T4):**

13. Custom plugin authoring, `NetworkTrackingPlugin`, `groupIdentify`, multiple SDK instances, manual `setSessionId`, COPPA mode, partner/plan metadata, custom HTTP client.

Each of steps 4–11 is mechanical — same interop pattern as what's already in the repo — but each iOS feature is a two-step change (Swift wrapper + C# call site) while each Android feature is one step (JNI reflection in `AmplitudeJniService`).

---

*Generated from a manual audit of the binding code against the published Amplitude SDK docs and the open-source SDK repos. Re-run when the binding surface changes.*
