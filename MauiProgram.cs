// AI-generated demo code. Not production-grade. Not maintained or
// supported by Amplitude. Intended for proof / prototyping only.
// See README.md for full context.

using Microsoft.Extensions.Logging;
using AmplitudeMauiPoc.Services;
using AmplitudeMauiPoc.Views;

namespace AmplitudeMauiPoc;

public static class MauiProgram
{
    // Amplitude API key resolution order:
    //   1. AMPLITUDE_API_KEY environment variable (useful for CI / local shell)
    //   2. .env file at the project root, bundled into the app (gitignored)
    //   3. Falls through to the placeholder — Amplitude won't initialize.
    // Never commit a real key. See README and .env.example for details.
    private const string AmplitudeApiKeyEnvVar = "AMPLITUDE_API_KEY";
    private const string AmplitudeApiKeyPlaceholder = "YOUR_AMPLITUDE_API_KEY_HERE";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Platform-specific Amplitude service registration
#if ANDROID
        builder.Services.AddSingleton<IAmplitudeService,
            Platforms.Android.AmplitudeJniService>();
#elif IOS
        builder.Services.AddSingleton<IAmplitudeService,
            Platforms.iOS.AmplitudeIOSService>();
#endif

        // Register pages
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ItemDetailPage>();

        var app = builder.Build();

        var apiKey = ResolveAmplitudeApiKey();
        if (apiKey != AmplitudeApiKeyPlaceholder)
        {
            var amplitude = app.Services.GetRequiredService<IAmplitudeService>();
            amplitude.Init(apiKey);
        }

        return app;
    }

    private static string ResolveAmplitudeApiKey()
    {
        var envKey = Environment.GetEnvironmentVariable(AmplitudeApiKeyEnvVar);
        if (!string.IsNullOrWhiteSpace(envKey))
            return envKey.Trim();

        var dotenv = LoadDotEnv();
        if (dotenv.TryGetValue(AmplitudeApiKeyEnvVar, out var fromFile)
            && !string.IsNullOrWhiteSpace(fromFile))
            return fromFile;

        return AmplitudeApiKeyPlaceholder;
    }

    private static Dictionary<string, string> LoadDotEnv()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("env")
                .GetAwaiter().GetResult();
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    continue;
                if (trimmed.StartsWith("export ", StringComparison.Ordinal))
                    trimmed = trimmed[7..].TrimStart();
                var eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;
                var key = trimmed[..eq].Trim();
                var value = trimmed[(eq + 1)..].Trim();
                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') ||
                     (value[0] == '\'' && value[^1] == '\'')))
                    value = value[1..^1];
                result[key] = value;
            }
        }
        catch
        {
            // .env not bundled or unreadable — caller falls through.
        }
        return result;
    }
}
