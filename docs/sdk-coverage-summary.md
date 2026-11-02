# Amplitude on .NET MAUI — Executive Summary

**From:** Amplitude Solutions Engineering
**For:** Mobile engineering team and leadership at the customer
**Companion document:** [`sdk-coverage.md`](sdk-coverage.md)
**Reading time:** ~3 minutes

---

## Purpose of this document

Your team is evaluating whether Amplitude can integrate cleanly into your existing .NET MAUI codebase. To de-risk that question ahead of any scoping work on your side, an Amplitude SE built a small proof-of-concept .NET MAUI application that integrates the **official Amplitude Android (Kotlin) and iOS (Swift) SDKs**, including **Session Replay**, from shared C# code.

This summary describes what the proof-of-concept demonstrates, where it stands today, and where we think your team would want to focus first when adapting the pattern for a production rollout. The detailed feature-by-feature gap analysis is in the companion document [`sdk-coverage.md`](sdk-coverage.md).

A note on provenance, in case you read the code: this proof-of-concept was generated with significant AI assistance, with limited human review, and is **not** an officially maintained Amplitude integration. We are sharing it as a working demonstration of the integration pattern — something for your engineers to read, run, and learn from — not as a library we expect you to drop into production. Your team should own the engineering for the production version, with us available to advise.

## What the proof-of-concept demonstrates

It answers one specific technical question: **can a .NET MAUI application send data to Amplitude using our native iOS and Android SDKs, instead of going through Amplitude's general-purpose HTTP ingestion API?**

The answer is yes. The pattern works end-to-end:

- Events tracked from shared C# code arrive in Amplitude correctly on both platforms.
- The user identity model (`setUserId`) is wired through to both platforms.
- Session Replay records sessions on both Android and iOS and they appear in the Amplitude dashboard.
- Build, package, and run on both platforms is functional with the standard .NET MAUI toolchain.

Practically, this means your team can get the same analytics and Session Replay capability that a native Android or iOS app would get from Amplitude — without leaving the C# codebase you already maintain. You're not required to rewrite parts of the app in Swift or Kotlin, and you're not required to fall back to the lower-fidelity HTTP ingestion API.

The integration approach uses the standard pattern Microsoft and the .NET MAUI Community Toolkit recommend for consuming native libraries from MAUI — there's nothing exotic or off-piste about it.

## Where the proof-of-concept stands today

The shared C# interface implements the **core analytics surface** that almost every Amplitude customer uses: initialization, event tracking with properties, identifying the current user, flushing the event queue, and installing Session Replay. Default behaviours the SDK handles automatically (session detection, app lifecycle events, screen-view capture) work without additional code.

It does **not yet implement** several features that most production analytics deployments add as they mature. The notable ones:

- **User properties** (the `Identify` API). Used for cohorts, segmentation, and most behavioural analysis. The majority of Amplitude customers add this within the first few weeks of an integration.
- **Logout / account-switching support** (`reset()`). One call, but skipping it means analytics events get attributed to the wrong user after a logout.
- **Consent management** (`optOut`). Required to comply with GDPR, CCPA, and similar privacy regimes. The current proof-of-concept has no way to disable tracking for a user who has not consented.
- **EU data residency** (`serverZone`). The proof-of-concept sends data to Amplitude's US region. If any of your users are in the EU, the SDK needs to be initialized with the EU zone.
- **Session Replay configuration.** Sample rate is hardcoded to 100% (every session is recorded) and no PII masking is configured. This is appropriate for a demo recording sessions in a test environment; it is **not** appropriate for real user traffic and would need adjusting before any pilot.

A second set of features is use-case-specific (revenue tracking for monetised apps, group analytics for B2B / multi-tenant apps, custom plugins, etc.) and only needs to be added if the corresponding use case applies to your app.

## What we think the path to a production pilot looks like

The gap between the proof-of-concept and a production-ready integration is **mechanical engineering work, not invention**. Every missing feature follows the same interop pattern that's already in the codebase, and the SDK functionality itself is already shipped on Maven Central and via Swift Package Manager — only the shared C# wrapper needs to be extended.

Based on what we see across other Amplitude customers, the priority sequence we'd suggest for your team to evaluate:

1. **Privacy and consent first.** Add `optOut`, wire it to your existing consent flow, and configure Session Replay privacy masking. Until this is done, the integration should not point at real user traffic. This is as much a legal/privacy review item as an engineering one.
2. **EU server zone**, if any of your data subjects are in the EU. This is a trivial code change, but a hard requirement.
3. **User properties + `reset()`.** Unlocks most production analytics use cases (cohorting, segmentation, logout handling).
4. **Session Replay sample-rate tuning.** Move off the hardcoded 100% value and pick a real production rate (typically 1–10%, depending on session volume).
5. Anything else (revenue, B2B groups, custom plugins) only if and when your product roadmap calls for it.

Final scope, sequencing, and effort estimates are decisions for your engineering team — we don't have a complete view of your codebase, your team's velocity, or your privacy/compliance posture. The list above is a starting point for that conversation, not a delivery plan.

## Our read on the technical risk

**Low.** The proof-of-concept demonstrates that the end-to-end mechanics work. The remaining work is bounded engineering — extending the shared C# wrapper to expose more of the SDK surface, and configuring Session Replay appropriately for your privacy obligations.

We suggest treating the proof-of-concept as a reference implementation of the integration pattern, not as a starting codebase you'd inherit directly. The patterns are correct and the build pipeline works; the production version is yours to design and own.

Happy to walk through the code, the gap analysis, or the priority sequence with your team whenever it's useful.

---

*This document reflects an Amplitude solutions-engineering view of the integration. The proof-of-concept it describes is AI-generated demo code, not a supported Amplitude product, and is not maintained by Amplitude. Final scoping, estimation, privacy review, and production engineering remain with the customer team.*
