using Newtonsoft.Json;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// Holds configuration targets and settings for system audio recording devices.
/// </summary>
public sealed class RecordingAudioProfile
{
    /// <summary>Gets or sets the target default multimedia recording device reference block.</summary>
    public AudioEndpointReference TargetDevice { get; set; } = new();

    /// <summary>Gets or sets the target default console recording device reference block.</summary>
    public AudioEndpointReference ConsoleDevice { get; set; } = new();

    /// <summary>Gets or sets the target default communications recording voice reference block.</summary>
    public AudioEndpointReference CommunicationsDevice { get; set; } = new();

    /// <summary>Gets or sets the input volume percentage level.</summary>
    public decimal VolumePercent { get; set; }

    /// <summary>Gets or sets a value indicating whether input channels are muted.</summary>
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

    /// <summary>Gets or sets the fine-grained custom effect slider configuration key-value mappings.</summary>
    public Dictionary<string, string> ApoSliders { get; set; } = new();

    /// <summary>Gets or sets a telemetry flag stating if recording features are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsRecordingEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if default multimedia routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultRecordingDeviceEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if default console routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultConsoleRecordingDeviceEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if voice communications routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultCommunicationsRecordingDeviceEnabled { get; set; }

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

    /// <summary>Gets or sets a telemetry flag stating if endpoint visibility management is enabled. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDeviceDisabledTrackingEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if multi-channel balance changes are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsChannelVolumeEnabled { get; set; }

    /// <summary>Gets or sets a tracking switch stating if custom slider configurations should be applied. Ignored in JSON.</summary>
    [JsonIgnore] public bool IsApoSlidersEnabled { get; set; }

    /// <summary>Ensures sub-elements avoid object ref fault allocations post serialization.</summary>
    public void EnsureDefaults()
    {
        TargetDevice ??= new AudioEndpointReference();
        ConsoleDevice ??= new AudioEndpointReference();
        CommunicationsDevice ??= new AudioEndpointReference();
        StreamFormat ??= new AudioFormatProfile();
        AudioEnhancements ??= new AudioEnhancementProfile();
        ApoSliders ??= new Dictionary<string, string>();

        if (TargetDevice.HardwareDetails == null)
        {
            TargetDevice.HardwareDetails = new HardwareDetails();
        }
        if (CommunicationsDevice.HardwareDetails == null)
        {
            CommunicationsDevice.HardwareDetails = new HardwareDetails();
        }
        if (ConsoleDevice.HardwareDetails == null)
        {
            ConsoleDevice.HardwareDetails = new HardwareDetails();
        }

        // Auto-hydrate default recording flags
        if (!string.IsNullOrWhiteSpace(TargetDevice.DeviceId))
        {
            IsRecordingEnabled = true;
            IsDefaultRecordingDeviceEnabled = true;
            IsVolumeEnabled = true;
            IsMuteEnabled = true;
            IsFormatEnabled = StreamFormat.SampleRate > 0;
            IsAudioEnhancementsEnabled = AudioEnhancements.AreEnhancementsSupported;
            TargetDevice.IsEndpointEnabled = true;

            IsDeviceDisabledTrackingEnabled = true;
            IsChannelVolumeEnabled = VolumeLeft > 0.0m || VolumeRight > 0.0m;
            IsApoSlidersEnabled = ApoSliders.Count > 0;
        }

        // Auto-hydrate console routing flag
        if (!string.IsNullOrWhiteSpace(ConsoleDevice.DeviceId))
        {
            IsRecordingEnabled = true;
            IsDefaultConsoleRecordingDeviceEnabled = true;
            ConsoleDevice.IsEndpointEnabled = true;
        }

        // Auto-hydrate communications recording flags
        if (!string.IsNullOrWhiteSpace(CommunicationsDevice.DeviceId))
        {
            IsRecordingEnabled = true;
            IsDefaultCommunicationsRecordingDeviceEnabled = true;
            CommunicationsDevice.IsEndpointEnabled = true;
        }
    }
}
