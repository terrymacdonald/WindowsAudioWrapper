using Newtonsoft.Json;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// The only profile/state object used by WindowsAudioWrapper.
/// A profile may be created by a user, saved in JSON, or captured from the current Windows audio state.
/// </summary>
public sealed class AudioProfile
{
    /// <summary>Gets or sets the settings associated with playback (Render) devices.</summary>
    public PlaybackAudioProfile Playback { get; set; } = new();

    /// <summary>Gets or sets the settings associated with recording (Capture) devices.</summary>
    public RecordingAudioProfile Recording { get; set; } = new();

    /// <summary>Gets or sets the system-wide audio settings.</summary>
    [JsonIgnore]
    public SystemAudioProfile System { get; set; } = new();

    /// <summary>Gets or sets the captured visibility state for all known audio endpoints.</summary>
    public List<AudioEndpointVisibility> EndpointVisibilities { get; set; } = new();

    /// <summary>Gets a value indicating whether any settings within the profile are marked to be applied.</summary>
    [JsonIgnore]
    public bool IsAnyAudioSettingEnabled =>
        Playback.IsPlaybackEnabled ||
        Recording.IsRecordingEnabled ||
        IsEndpointVisibilityTrackingEnabled ||
        System.IsSystemAudioEnabled;

    /// <summary>Gets or sets whether endpoint visibility states should be applied. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsEndpointVisibilityTrackingEnabled { get; set; }

    /// <summary>Ensures all nested objects are instantiated properly after deserialization.</summary>
    public void EnsureDefaults()
    {
        Playback ??= new PlaybackAudioProfile();
        Recording ??= new RecordingAudioProfile();
        System ??= new SystemAudioProfile();
        EndpointVisibilities ??= new List<AudioEndpointVisibility>();

        Playback.EnsureDefaults();
        Recording.EnsureDefaults();
        System.EnsureDefaults();

        if (EndpointVisibilities.Count > 0)
        {
            IsEndpointVisibilityTrackingEnabled = true;
        }
    }

    /// <summary>
    /// Flips every hidden control flag across playback, recording, and system blocks to true.
    /// Call this immediately after deserializing a clean JSON file to perform a full macro profile apply.
    /// </summary>
    public void EnableAllFeatures()
    {
        EnsureDefaults();

        Playback.IsPlaybackEnabled = true;
        Playback.IsDefaultPlaybackDeviceEnabled = !string.IsNullOrWhiteSpace(Playback.MultimediaDevice.DeviceId);
        Playback.IsDefaultConsolePlaybackDeviceEnabled = !string.IsNullOrWhiteSpace(Playback.ConsoleDevice.DeviceId);
        Playback.IsDefaultCommunicationsPlaybackDeviceEnabled = !string.IsNullOrWhiteSpace(Playback.CommunicationsDevice.DeviceId);
        Playback.IsVolumeEnabled = true;
        Playback.IsMuteEnabled = true;
        Playback.IsFormatEnabled = Playback.StreamFormat.SampleRate > 0;
        Playback.IsAudioEnhancementsEnabled = true;
        Playback.IsChannelVolumeEnabled = true;

        Recording.IsRecordingEnabled = true;
        Recording.IsDefaultRecordingDeviceEnabled = !string.IsNullOrWhiteSpace(Recording.MultimediaDevice.DeviceId);
        Recording.IsDefaultConsoleRecordingDeviceEnabled = !string.IsNullOrWhiteSpace(Recording.ConsoleDevice.DeviceId);
        Recording.IsDefaultCommunicationsRecordingDeviceEnabled = !string.IsNullOrWhiteSpace(Recording.CommunicationsDevice.DeviceId);
        Recording.IsVolumeEnabled = true;
        Recording.IsMuteEnabled = true;
        Recording.IsFormatEnabled = Recording.StreamFormat.SampleRate > 0;
        Recording.IsAudioEnhancementsEnabled = true;
        Recording.IsChannelVolumeEnabled = true;

        IsEndpointVisibilityTrackingEnabled = EndpointVisibilities.Count > 0;

        System.IsSystemAudioEnabled = true;
        System.IsMonoAudioEnabled = true;
    }

    
}
