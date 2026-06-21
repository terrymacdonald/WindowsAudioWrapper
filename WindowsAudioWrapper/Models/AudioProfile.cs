using System.Text.Json.Serialization;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// The only profile/state object used by WindowsAudioWrapper.
/// A profile may be created by a user, saved in JSON, or captured from the current Windows audio state.
/// </summary>
public sealed class AudioProfile: IEquatable<AudioProfile>
{
    /// <summary>Gets or sets the user-friendly name of the audio profile.</summary>
    public string ProfileName { get; set; } = "Current Windows Audio Profile";

    /// <summary>Gets or sets a value indicating whether this audio profile is currently applied and active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the schema version of the profile, useful for backwards compatibility.</summary>
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
        Playback.IsDefaultPlaybackDeviceEnabled = !string.IsNullOrWhiteSpace(Playback.TargetDevice.DeviceId);
        Playback.IsDefaultCommunicationsPlaybackDeviceEnabled = !string.IsNullOrWhiteSpace(Playback.CommunicationsDevice.DeviceId);
        Playback.IsVolumeEnabled = true;
        Playback.IsMuteEnabled = true;
        Playback.IsFormatEnabled = Playback.StreamFormat.SampleRate > 0;
        Playback.IsAudioEnhancementsEnabled = true;
        Playback.IsSpatialAudioEnabled = true;
        Playback.IsDeviceDisabledTrackingEnabled = true;
        Playback.IsChannelVolumeEnabled = true;
        Playback.IsApoSlidersEnabled = true;

        Recording.IsRecordingEnabled = true;
        Recording.IsDefaultRecordingDeviceEnabled = !string.IsNullOrWhiteSpace(Recording.TargetDevice.DeviceId);
        Recording.IsDefaultCommunicationsRecordingDeviceEnabled = !string.IsNullOrWhiteSpace(Recording.CommunicationsDevice.DeviceId);
        Recording.IsVolumeEnabled = true;
        Recording.IsMuteEnabled = true;
        Recording.IsFormatEnabled = Recording.StreamFormat.SampleRate > 0;
        Recording.IsAudioEnhancementsEnabled = true;
        Recording.IsSpatialAudioEnabled = true;
        Recording.IsDeviceDisabledTrackingEnabled = true;
        Recording.IsChannelVolumeEnabled = true;
        Recording.IsApoSlidersEnabled = true;

        System.IsSystemAudioEnabled = true;
        System.IsMonoAudioEnabled = true;
    }

    /// <inheritdoc/>
    public bool Equals(AudioProfile? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // Custom Semantic Equality Check: ProfileName is intentionally skipped
        return IsActive == other.IsActive &&
               SchemaVersion == other.SchemaVersion &&
               EqualityComparer<PlaybackAudioProfile>.Default.Equals(Playback, other.Playback) &&
               EqualityComparer<RecordingAudioProfile>.Default.Equals(Recording, other.Recording);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as AudioProfile);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(IsActive, SchemaVersion, Playback, Recording);
    }

    /// <summary>Compares two AudioProfile objects for equality.</summary>
    public static bool operator ==(AudioProfile? left, AudioProfile? right) => Equals(left, right);

    /// <summary>Compares two AudioProfile objects for inequality.</summary>
    public static bool operator !=(AudioProfile? left, AudioProfile? right) => !Equals(left, right);
}