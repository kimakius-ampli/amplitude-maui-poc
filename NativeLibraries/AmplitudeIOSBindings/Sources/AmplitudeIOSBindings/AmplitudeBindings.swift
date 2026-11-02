import Foundation
import AmplitudeSwift
import AmplitudeSwiftSessionReplayPlugin

/// Slim @objc wrapper around AmplitudeSwift + AmplitudeSessionReplay so that
/// .NET MAUI can call into the SDK via ObjCRuntime without needing a full
/// Swift binding.
///
/// The ObjC selectors generated from this class are stable contracts the
/// C# AmplitudeIOSService relies on. Do not rename without updating the
/// C# side in lockstep.
@objc(AMPMauiAmplitude)
@objcMembers
public final class AmplitudeBindings: NSObject {

    private var amplitude: Amplitude?
    public private(set) var isSessionReplayActive: Bool = false

    public override init() {
        super.init()
    }

    /// ObjC selector: `initializeWithApiKey:serverZone:sampleRate:`
    public func initialize(apiKey: String, serverZone: String, sampleRate: Double) {
        let zone: ServerZone = serverZone.uppercased() == "EU" ? .EU : .US

        let config = Configuration(
            apiKey: apiKey,
            serverZone: zone
        )

        let amp = Amplitude(configuration: config)

        let srPlugin = AmplitudeSwiftSessionReplayPlugin(sampleRate: Float(sampleRate))
        _ = amp.add(plugin: srPlugin)

        self.amplitude = amp
        self.isSessionReplayActive = true
    }

    /// ObjC selector: `trackEvent:propertiesJson:`
    /// Properties are passed as a JSON-encoded string for ABI simplicity.
    /// Pass nil or an empty string for no properties.
    public func trackEvent(_ event: String, propertiesJson: String?) {
        guard let amp = amplitude else { return }

        var props: [String: Any]? = nil
        if let json = propertiesJson, !json.isEmpty,
           let data = json.data(using: .utf8),
           let parsed = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
            props = parsed
        }

        _ = amp.track(eventType: event, eventProperties: props)
    }

    /// ObjC selector: `setUserId:`
    public func setUserId(_ userId: String) {
        _ = amplitude?.setUserId(userId: userId)
    }

    /// ObjC selector: `flush`
    public func flush() {
        _ = amplitude?.flush()
    }
}
