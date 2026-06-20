using System.Text.Json.Serialization;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// Holds configuration targets and settings for system audio playback devices.
/// </summary>
public sealed class PlaybackAudioProfile
{
    /// <summary>Gets or sets the target default multimedia playback device reference block.</summary>
    public AudioEndpointReference TargetDevice { get; set; } = new();

    /// <summary>Gets or sets the target default communications voice routing reference block.</summary>
    public AudioEndpointReference CommunicationsDevice { get; set; } = new();

    /// <summary>Gets or sets the output volume percentage level.</summary>
    public decimal VolumePercent { get; set; }

    /// <summary>Gets or sets a value indicating whether output channels are muted.</summary>
    public bool IsMuted { get; set; }

    /// <summary>Gets or sets the stream layout sample frequencies and layouts.</summary>
    public AudioFormatProfile StreamFormat { get; set; } = new();

    /// <summary>Gets or sets APO system enhancement profiles.</summary>
    public AudioEnhancementProfile AudioEnhancements { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether the target device should be completely disabled/hidden in the OS.</summary>
    public bool IsDeviceDisabled { get; set; } = false;

    /// <summary>Gets or sets the discrete Left channel volume level percentage (0-100).</summary>
    public decimal VolumeLeft { get; set; } = 0.0m;

    /// <summary>Gets or sets the discrete Right channel volume level percentage (0-100).</summary>
    public decimal VolumeRight { get; set; } = 0.0m;

    /// <summary>Gets or sets a telemetry flag stating if playback features are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsPlaybackEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if default multimedia routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultPlaybackDeviceEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if voice communications routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultCommunicationsPlaybackDeviceEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if hardware volume lines are readable. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsVolumeEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if hardware mute lines are readable. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsMuteEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if device format arrays are readable. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsFormatEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if device APO switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsAudioEnhancementsEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if spatial audio changes are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsSpatialAudioEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if endpoint visibility management is enabled. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDeviceDisabledTrackingEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if multi-channel balance changes are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsChannelVolumeEnabled { get; set; }

    /// <summary>Ensures sub-elements avoid object ref fault allocations post serialization.</summary>
    public void EnsureDefaults()
    {
        TargetDevice ??= new AudioEndpointReference();
        CommunicationsDevice ??= new AudioEndpointReference();
        StreamFormat ??= new AudioFormatProfile();
        AudioEnhancements ??= new AudioEnhancementProfile();

        if (TargetDevice.HardwareDetails == null)
        {
            TargetDevice.HardwareDetails = new HardwareDetails();
        }
        if (CommunicationsDevice.HardwareDetails == null)
        {
            CommunicationsDevice.HardwareDetails = new HardwareDetails();
        }

        // Auto-hydrate default multimedia flags
        if (!string.IsNullOrWhiteSpace(TargetDevice.DeviceId))
        {
            IsPlaybackEnabled = true;
            IsDefaultPlaybackDeviceEnabled = true;
            IsVolumeEnabled = true;
            IsMuteEnabled = true;
            IsFormatEnabled = StreamFormat.SampleRate > 0;
            IsAudioEnhancementsEnabled = AudioEnhancements.AreEnhancementsSupported;
            TargetDevice.IsEndpointEnabled = true;
            IsSpatialAudioEnabled = !string.IsNullOrWhiteSpace(TargetDevice.HardwareDetails.SpatialAudioFormat);
            
            IsDeviceDisabledTrackingEnabled = true;
            IsChannelVolumeEnabled = VolumeLeft > 0.0m || VolumeRight > 0.0m;
        }

        // Auto-hydrate communications routing flags
        if (!string.IsNullOrWhiteSpace(CommunicationsDevice.DeviceId))
        {
            IsPlaybackEnabled = true;
            IsDefaultCommunicationsPlaybackDeviceEnabled = true;
            CommunicationsDevice.IsEndpointEnabled = true;
        }
    }
}