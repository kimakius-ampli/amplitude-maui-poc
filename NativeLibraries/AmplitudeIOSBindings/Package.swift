// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AmplitudeIOSBindings",
    platforms: [.iOS(.v16)],
    products: [
        .library(
            name: "AmplitudeIOSBindings",
            type: .dynamic,
            targets: ["AmplitudeIOSBindings"]
        ),
    ],
    dependencies: [
        // AmplitudeSwift: analytics SDK
        .package(url: "https://github.com/amplitude/Amplitude-Swift.git", from: "1.13.0"),
        // Session Replay plugin
        .package(url: "https://github.com/amplitude/AmplitudeSessionReplay-iOS.git", from: "0.7.0"),
    ],
    targets: [
        .target(
            name: "AmplitudeIOSBindings",
            dependencies: [
                .product(name: "AmplitudeSwift", package: "Amplitude-Swift"),
                .product(name: "AmplitudeSwiftSessionReplayPlugin", package: "AmplitudeSessionReplay-iOS"),
            ],
            path: "Sources/AmplitudeIOSBindings"
        ),
    ]
)
