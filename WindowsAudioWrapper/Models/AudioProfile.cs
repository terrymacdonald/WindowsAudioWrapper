using System.Text.Json.Serialization;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// The only profile/state object used by WindowsAudioWrapper.
/// A profile may be created by a user, saved in JSON, or captured from the current Windows audio state.
/// </summary>
public sealed class AudioProfile
{
    /// <summary>Gets or sets the user-friendly name of the audio profile.</summary>
    public string ProfileName { get; set; } = "Ultra-Fidelity Studio Monitor";

    /// <summary>Gets or sets a value indicating whether this audio profile is currently applied and active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the schema version of the profile, useful for backwards compatibility.</summary>
    [JsonIgnore]
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Gets or sets the settings associated with playback (Render) devices.</summary>
    public PlaybackAudioProfile Playback { get; set; } = new();

    /// <summary>Gets or sets the settings associated with recording (Capture) devices.</summary>
    public RecordingAudioProfile Recording { get; set; } = new();

    /// <summary>Gets or sets the system-wide audio settings.</summary>
    [JsonIgnore]
    public SystemAudioProfile System { get; set; } = new();

    /// <summary>Gets a value indicating whether any settings within the profile are marked to be applied.</summary>
    [JsonIgnore]
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

    /// <summary>
    /// Flips every hidden control flag across playback, recording, and system blocks to true.
    /// Call this immediately after deserializing a clean JSON file to perform a full macro profile apply.
    /// </summary>
    public void EnableAllFeatures()
    {
        EnsureDefaults();

        Playback.IsPlaybackEnabled = true;
        Playback.IsDefaultPlaybackDeviceEnabled = true;
        Playback.IsDefaultCommunicationsPlaybackDeviceEnabled = true;
        Playback.IsVolumeEnabled = true;
        Playback.IsMuteEnabled = true;
        Playback.IsFormatEnabled = Playback.StreamFormat.SampleRate > 0;
        Playback.IsAudioEnhancementsEnabled = true;

        Recording.IsRecordingEnabled = true;
        Recording.IsDefaultRecordingDeviceEnabled = true;
        Recording.IsDefaultCommunicationsRecordingDeviceEnabled = true;
        Recording.IsVolumeEnabled = true;
        Recording.IsMuteEnabled = true;
        Recording.IsFormatEnabled = Recording.StreamFormat.SampleRate > 0;
        Recording.IsAudioEnhancementsEnabled = true;

        System.IsSystemAudioEnabled = true;
        System.IsMonoAudioEnabled = true;
    }
}