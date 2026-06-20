using System.Text.Json.Serialization;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// Holds configuration targets and unmanaged telemetry fields for system audio recording devices.
/// </summary>
public sealed class RecordingAudioProfile
{
    /// <summary>Gets or sets the target recording device reference block.</summary>
    public AudioEndpointReference TargetDevice { get; set; } = new();

    /// <summary>Gets or sets the input volume percentage level.</summary>
    public decimal VolumePercent { get; set; }

    /// <summary>Gets or sets a value indicating whether input channels are muted.</summary>
    public bool IsMuted { get; set; }

    /// <summary>Gets or sets the stream layout sample frequencies and layouts.</summary>
    public AudioFormatProfile StreamFormat { get; set; } = new();

    /// <summary>Gets or sets APO system enhancement profiles.</summary>
    public AudioEnhancementProfile AudioEnhancements { get; set; } = new();

    /// <summary>Gets or sets a tracking token stating if communications hardware routing is active. Ignored in JSON.</summary>
    [JsonIgnore]
    public AudioEndpointReference CommunicationsDevice { get; set; } = new();

    /// <summary>Gets or sets a telemetry flag stating if recording features are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsRecordingEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if default routing switches are active. Ignored in JSON.</summary>
    [JsonIgnore]
    public bool IsDefaultRecordingDeviceEnabled { get; set; }

    /// <summary>Gets or sets a telemetry flag stating if voice routing switches are active. Ignored in JSON.</summary>
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

    /// <summary>Ensures sub-elements avoid object ref fault allocations post serialization.</summary>
    public void EnsureDefaults()
    {
        TargetDevice ??= new AudioEndpointReference();
        StreamFormat ??= new AudioFormatProfile();
        AudioEnhancements ??= new AudioEnhancementProfile();
        CommunicationsDevice ??= new AudioEndpointReference();

        // Automatically hydrate hidden validation/apply flags when loading from clean JSON configurations
        if (!string.IsNullOrWhiteSpace(TargetDevice.DeviceId))
        {
            IsRecordingEnabled = true;
            IsDefaultRecordingDeviceEnabled = true;
            IsVolumeEnabled = true;
            IsMuteEnabled = true;
            IsFormatEnabled = StreamFormat.SampleRate > 0;
            IsAudioEnhancementsEnabled = AudioEnhancements.AreEnhancementsSupported;
            
            // Fixes the JSON cloning validation drop issue
            TargetDevice.IsEndpointEnabled = true;
        }

        if (!string.IsNullOrWhiteSpace(CommunicationsDevice.DeviceId))
        {
            IsDefaultCommunicationsRecordingDeviceEnabled = true;
            CommunicationsDevice.IsEndpointEnabled = true;
        }
    }
}