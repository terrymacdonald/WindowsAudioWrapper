using System.Text.Json.Serialization;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// The master audio configuration profile handled by WindowsAudioWrapper.
/// Maps natively to the clean configuration JSON schema while maintaining compatibility.
/// </summary>
public sealed class AudioProfile
{
    /// <summary>Gets or sets the name of the profile configuration context.</summary>
    public string ProfileName { get; set; } = "Ultra-Fidelity Studio Monitor";

    /// <summary>Gets or sets a value indicating whether this profile layout is applied and active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the configuration schema data associated with playback (Render) devices.</summary>
    public PlaybackAudioProfile Playback { get; set; } = new();

    /// <summary>Gets or sets the configuration schema data associated with recording (Capture) devices.</summary>
    public RecordingAudioProfile Recording { get; set; } = new();

    /// <summary>Gets or sets the system-wide audio settings. Hidden from standard target JSON via ignore filters.</summary>
    [JsonIgnore]
    public SystemAudioProfile System { get; set; } = new();

    /// <summary>Gets or sets the schema version of the profile layout context. Hidden from standard target JSON.</summary>
    [JsonIgnore]
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Gets a value indicating whether any settings within the profile are marked to be applied.</summary>
    [JsonIgnore]
    public bool IsAnyAudioSettingEnabled =>
        Playback.IsPlaybackEnabled ||
        Recording.IsRecordingEnabled ||
        System.IsSystemAudioEnabled;

    /// <summary>Ensures all nested objects are instantiated properly after deserialization sweeps.</summary>
    public void EnsureDefaults()
    {
        Playback ??= new PlaybackAudioProfile();
        Recording ??= new RecordingAudioProfile();
        System ??= new SystemAudioProfile();

        Playback.EnsureDefaults();
        Recording.EnsureDefaults();
        System.EnsureDefaults();
    }
}