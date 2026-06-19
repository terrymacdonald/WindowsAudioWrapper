namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the requested state for recording devices within an AudioProfile.
/// </summary>
public sealed class RecordingAudioProfile
{
    /// <summary>Gets or sets whether any recording settings should be evaluated and applied.</summary>
    public bool IsRecordingEnabled { get; set; } = false;

    /// <summary>Gets or sets the reference to the targeted primary recording device.</summary>
    public AudioEndpointReference Device { get; set; } = new();

    /// <summary>Gets or sets the reference to the targeted communications recording device.</summary>
    public AudioEndpointReference CommunicationsDevice { get; set; } = new();

    /// <summary>Gets or sets whether the default multimedia recording device should be changed.</summary>
    public bool IsDefaultRecordingDeviceEnabled { get; set; } = false;

    /// <summary>Gets or sets whether the default communications recording device should be changed.</summary>
    public bool IsDefaultCommunicationsRecordingDeviceEnabled { get; set; } = false;

    /// <summary>Gets or sets whether the target device's volume should be changed.</summary>
    public bool IsVolumeEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired volume percentage (0-100).</summary>
    public decimal VolumePercent { get; set; } = 50;

    /// <summary>Gets or sets whether the target device's mute state should be changed.</summary>
    public bool IsMuteEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired mute state.</summary>
    public bool IsMuted { get; set; } = false;

    /// <summary>Gets or sets whether the target device's default format should be changed.</summary>
    public bool IsFormatEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired audio format profile.</summary>
    public AudioFormatProfile Format { get; set; } = new();

    /// <summary>Gets or sets whether the target device's audio enhancements should be changed.</summary>
    public bool IsAudioEnhancementsEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired audio enhancement profile.</summary>
    public AudioEnhancementProfile AudioEnhancements { get; set; } = new();

    /// <summary>Gets or sets whether the target device's voice processing settings should be changed.</summary>
    public bool IsVoiceProcessingEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired voice processing profile.</summary>
    public VoiceProcessingProfile VoiceProcessing { get; set; } = new();

    /// <summary>Ensures all nested objects are instantiated properly after deserialization.</summary>
    public void EnsureDefaults()
    {
        Device ??= new AudioEndpointReference();
        CommunicationsDevice ??= new AudioEndpointReference();
        Format ??= new AudioFormatProfile();
        AudioEnhancements ??= new AudioEnhancementProfile();
        VoiceProcessing ??= new VoiceProcessingProfile();
    }
}