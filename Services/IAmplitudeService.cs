namespace AmplitudeMauiPoc.Services;

/// <summary>
/// Cross-platform Amplitude interface. Shared MAUI code programs against
/// this; each platform provides a native implementation underneath.
/// </summary>
public interface IAmplitudeService
{
    void Init(string apiKey);
    void Track(string eventName, Dictionary<string, object>? properties = null);
    void SetUserId(string userId);
    void Flush();
    bool IsSessionReplayActive { get; }
}
