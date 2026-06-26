using Newtonsoft.Json;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// Holds configuration targets and settings for system audio playback devices.
/// </summary>
public sealed class PlaybackAudioProfile
{
    /// <summary>Gets or sets the target default multimedia playback device reference block.</summary>
    public AudioEndpointReference MultimediaDevice { get; set; } = new();

    /// <summary>Gets or sets the target default console playback device reference block.</summary>
    public AudioEndpointReference ConsoleDevice { get; set; } = new();

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

    /// <summary>Gets or sets the discrete Left channel volume level percentage (0-100).</summary>
    public decimal VolumeLeft { get; set; } = 0.0m;

    /// <summary>Gets or sets the discrete Right channel volume level percentage (0-100).</summary>
    public decimal VolumeRight { get; set; } = 0.0m;

    /// <summary>Gets or sets the fine-grained custom effect slider configuration key-value mappings.</summary>
    public Dictionary<string, string> ApoSliders { get; set; } = new();

    /// <summary>Gets or sets a telemetry flag stating if playback features are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsPlaybackEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if default multimedia routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultPlaybackDeviceEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if default console routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultConsolePlaybackDeviceEnabled { get; set; }

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

    /// <summary>Gets or sets a telemetry flag stating if multi-channel balance changes are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsChannelVolumeEnabled { get; set; }

    /// <summary>Gets or sets a tracking switch stating if custom slider configurations should be applied. Ignored in JSON.</summary>
    [JsonIgnore] public bool IsApoSlidersEnabled { get; set; }

    /// <summary>Ensures sub-elements avoid object ref fault allocations post serialization.</summary>
    public void EnsureDefaults()
    {
        MultimediaDevice ??= new AudioEndpointReference();
        ConsoleDevice ??= new AudioEndpointReference();
        CommunicationsDevice ??= new AudioEndpointReference();
        StreamFormat ??= new AudioFormatProfile();
        AudioEnhancements ??= new AudioEnhancementProfile();
        ApoSliders ??= new Dictionary<string, string>();

        if (MultimediaDevice.HardwareDetails == null)
        {
            MultimediaDevice.HardwareDetails = new HardwareDetails();
        }
        if (CommunicationsDevice.HardwareDetails == null)
        {
            CommunicationsDevice.HardwareDetails = new HardwareDetails();
        }
        if (ConsoleDevice.HardwareDetails == null)
        {
            ConsoleDevice.HardwareDetails = new HardwareDetails();
        }

        // Auto-hydrate default multimedia flags
        if (!string.IsNullOrWhiteSpace(MultimediaDevice.DeviceId))
        {
            IsPlaybackEnabled = true;
            IsDefaultPlaybackDeviceEnabled = true;
            IsVolumeEnabled = true;
            IsMuteEnabled = true;
            IsFormatEnabled = StreamFormat.SampleRate > 0;
            IsAudioEnhancementsEnabled = AudioEnhancements.AreEnhancementsSupported;
            MultimediaDevice.IsEndpointEnabled = true;
            
            IsChannelVolumeEnabled = VolumeLeft > 0.0m || VolumeRight > 0.0m;

            // Activate custom sliders track if configuration records exist
            IsApoSlidersEnabled = ApoSliders.Count > 0;
        }

        // Auto-hydrate console routing flag
        if (!string.IsNullOrWhiteSpace(ConsoleDevice.DeviceId))
        {
            IsPlaybackEnabled = true;
            IsDefaultConsolePlaybackDeviceEnabled = true;
            ConsoleDevice.IsEndpointEnabled = true;
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
