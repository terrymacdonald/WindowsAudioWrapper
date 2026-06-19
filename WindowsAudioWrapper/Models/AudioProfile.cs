namespace WindowsAudioWrapper.Models;

/// <summary>
/// The only profile/state object used by WindowsAudioWrapper.
/// A profile may be created by a user, saved in JSON, or captured from the current Windows audio state.
/// </summary>
public sealed class AudioProfile
{
    /// <summary>Gets or sets the schema version of the profile, useful for backwards compatibility.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Gets or sets the settings associated with playback (Render) devices.</summary>
    public PlaybackAudioProfile Playback { get; set; } = new();

    /// <summary>Gets or sets the settings associated with recording (Capture) devices.</summary>
    public RecordingAudioProfile Recording { get; set; } = new();

    /// <summary>Gets or sets the system-wide audio settings.</summary>
    public SystemAudioProfile System { get; set; } = new();

    /// <summary>Gets a value indicating whether any settings within the profile are marked to be applied.</summary>
    public bool IsAnyAudioSettingEnabled =>
        Playback.IsPlaybackEnabled ||
        Recording.IsRecordingEnabled ||
        System.IsSystemAudioEnabled;

    /// <summary>Ensures all nested objects are instantiated properly after deserialization.</summary>
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