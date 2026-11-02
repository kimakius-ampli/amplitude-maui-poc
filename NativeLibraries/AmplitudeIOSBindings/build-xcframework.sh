#!/bin/bash
#
# Build AmplitudeIOSBindings.xcframework from the Swift package.
#
# Produces:
#   ./build/AmplitudeIOSBindings.xcframework
#
# Requirements:
#   - macOS with Xcode 15+ installed (xcodebuild understands Package.swift directly)
#   - Internet access (fetches AmplitudeSwift + AmplitudeSessionReplay via SPM)
#
# Run from the directory containing this script:
#   cd NativeLibraries/AmplitudeIOSBindings
#   ./build-xcframework.sh

set -euo pipefail

SCHEME="AmplitudeIOSBindings"
HERE="$(cd "$(dirname "$0")" && pwd)"
BUILD_DIR="$HERE/build"
DEVICE_ARCHIVE="$BUILD_DIR/ios-device"
SIM_ARCHIVE="$BUILD_DIR/ios-simulator"
XCFRAMEWORK="$BUILD_DIR/${SCHEME}.xcframework"

echo "==> Cleaning previous build output"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR"

echo "==> Archiving for iOS device"
xcodebuild archive \
    -scheme "$SCHEME" \
    -destination "generic/platform=iOS" \
    -archivePath "$DEVICE_ARCHIVE" \
    -derivedDataPath "$BUILD_DIR/derived-data-device" \
    SKIP_INSTALL=NO \
    ONLY_ACTIVE_ARCH=NO

echo "==> Archiving for iOS simulator"
xcodebuild archive \
    -scheme "$SCHEME" \
    -destination "generic/platform=iOS Simulator" \
    -archivePath "$SIM_ARCHIVE" \
    -derivedDataPath "$BUILD_DIR/derived-data-sim" \
    SKIP_INSTALL=NO \
    ONLY_ACTIVE_ARCH=NO

# SPM dynamic libraries archive into Products/usr/local/lib/ rather than the
# usual Library/Frameworks/ — pick the one that exists.
find_framework() {
    local archive="$1"
    for candidate in \
        "$archive.xcarchive/Products/usr/local/lib/${SCHEME}.framework" \
        "$archive.xcarchive/Products/Library/Frameworks/${SCHEME}.framework"; do
        [ -d "$candidate" ] && echo "$candidate" && return 0
    done
    return 1
}
DEVICE_FRAMEWORK="$(find_framework "$DEVICE_ARCHIVE")"
SIM_FRAMEWORK="$(find_framework "$SIM_ARCHIVE")"

if [ ! -d "$DEVICE_FRAMEWORK" ] || [ ! -d "$SIM_FRAMEWORK" ]; then
    echo "ERROR: expected frameworks not found after archive step."
    echo "  device: $DEVICE_FRAMEWORK"
    echo "  sim:    $SIM_FRAMEWORK"
    echo
    echo "Check the xcarchive contents and adjust paths if Xcode's layout has shifted."
    exit 1
fi

echo "==> Combining into xcframework"
xcodebuild -create-xcframework \
    -framework "$DEVICE_FRAMEWORK" \
    -framework "$SIM_FRAMEWORK" \
    -output "$XCFRAMEWORK"

echo "==> Copying transitive binary xcframeworks"
# AmplitudeCore and AmplitudeSessionReplay are shipped by Amplitude as
# pre-built binary xcframeworks. Our wrapper links against them
# dynamically, so they must travel with us into the MAUI app bundle.
DEP_ROOT="$BUILD_DIR/derived-data-device/SourcePackages"
for src in \
    "$DEP_ROOT/artifacts/amplitudecore-swift/AmplitudeCoreFramework/AmplitudeCore.xcframework" \
    "$DEP_ROOT/checkouts/AmplitudeSessionReplay-iOS/Frameworks/AmplitudeSessionReplay.xcframework"; do
    if [ ! -d "$src" ]; then
        echo "  Skipping (not found): $src"
        continue
    fi
    name="$(basename "$src")"
    rm -rf "$BUILD_DIR/$name"
    cp -R "$src" "$BUILD_DIR/$name"
    echo "  Copied: $name"
done

echo
echo "Built: $XCFRAMEWORK"
echo "Sibling xcframeworks for transitive deps copied alongside."
