#!/bin/bash
#
# Amplitude .NET MAUI PoC — Android quick start.
#
# AI-generated demo code. Not production-grade, not Amplitude-supported.
# See README.md for full context.
#
# Run from the repository root (the directory containing AmplitudeMauiPoc.csproj).

set -e

echo "=== Amplitude .NET MAUI PoC (Android quick start) ==="
echo ""

# Check prerequisites
echo "Checking prerequisites..."

if ! command -v dotnet &> /dev/null; then
    echo "ERROR: dotnet CLI not found. Install .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "  .NET SDK: $DOTNET_VERSION"

if [[ ! "$DOTNET_VERSION" == 10.* ]]; then
    echo "  WARNING: .NET 10 recommended. You have $DOTNET_VERSION"
    echo "  AndroidMavenLibrary requires .NET 9+, project targets net10.0."
fi

# Check MAUI workload
if ! dotnet workload list 2>/dev/null | grep -q "maui"; then
    echo "  Installing MAUI workload..."
    sudo dotnet workload install maui
fi
echo "  MAUI workload: installed"

# Check Android SDK
if [ -z "$ANDROID_HOME" ]; then
    # Try common paths
    if [ -d "$HOME/Library/Android/sdk" ]; then
        export ANDROID_HOME="$HOME/Library/Android/sdk"
    elif [ -d "$HOME/Android/Sdk" ]; then
        export ANDROID_HOME="$HOME/Android/Sdk"
    else
        echo "  WARNING: ANDROID_HOME not set. Set it to your Android SDK path."
    fi
fi
echo "  ANDROID_HOME: ${ANDROID_HOME:-NOT SET}"

# Check API key configuration
if [ -n "${AMPLITUDE_API_KEY:-}" ]; then
    echo "  Amplitude API key: from AMPLITUDE_API_KEY env var"
elif [ -f .env ] && grep -q '^AMPLITUDE_API_KEY=' .env && ! grep -q '^AMPLITUDE_API_KEY=YOUR_AMPLITUDE_API_KEY_HERE' .env; then
    echo "  Amplitude API key: from .env"
else
    echo ""
    echo "ACTION REQUIRED: Set your Amplitude API key before running."
    echo "  Either: export AMPLITUDE_API_KEY=<your-key>"
    echo "  Or:     cp .env.example .env  &&  edit .env"
    echo ""
    read -p "Press Enter after setting the API key..."
fi

# Build
echo ""
echo "Building for Android..."
dotnet restore
dotnet build -f net10.0-android

echo ""
echo "Build successful!"
echo ""

# Check for running emulator
if command -v adb &> /dev/null; then
    DEVICES=$(adb devices | grep -v "List" | grep "device" | wc -l | tr -d ' ')
    if [ "$DEVICES" -eq "0" ]; then
        echo "No Android device/emulator detected."
        echo "Start an emulator from Android Studio Device Manager, then run:"
        echo "  dotnet build -t:Run -f net10.0-android"
    else
        echo "Android device detected. Deploying..."
        dotnet build -t:Run -f net10.0-android
    fi
else
    echo "adb not found. Start an emulator and run:"
    echo "  dotnet build -t:Run -f net10.0-android"
fi
