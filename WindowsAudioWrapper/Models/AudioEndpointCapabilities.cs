namespace WindowsAudioWrapper.Models;

/// <summary>
/// Defines which features and settings a specific audio endpoint supports.
/// </summary>
public sealed class AudioEndpointCapabilities
{
    /// <summary>Gets or sets whether the device can be set as the default multimedia device.</summary>
    public bool IsDefaultDeviceSupported { get; set; } = true;

    /// <summary>Gets or sets whether the device can be set as the default communications device.</summary>
    public bool IsDefaultCommunicationsDeviceSupported { get; set; } = true;

    /// <summary>Gets or sets whether the device supports programmatic volume control.</summary>
    public bool IsVolumeSupported { get; set; } = true;

    /// <summary>Gets or sets whether the device supports programmatic muting.</summary>
    public bool IsMuteSupported { get; set; } = true;

    /// <summary>Gets or sets whether the device's default audio format can be read.</summary>
    public bool IsFormatReadSupported { get; set; } = false;

    /// <summary>Gets or sets whether the device's default audio format can be changed.</summary>
    public bool IsFormatSetSupported { get; set; } = false;

    /// <summary>Gets or sets whether spatial sound settings can be read from the device.</summary>
    public bool IsSpatialSoundReadSupported { get; set; } = false;

    /// <summary>Gets or sets whether spatial sound settings can be applied to the device.</summary>
    public bool IsSpatialSoundSetSupported { get; set; } = false;

    /// <summary>Gets or sets whether audio enhancement settings can be read.</summary>
    public bool IsAudioEnhancementsReadSupported { get; set; } = false;

    /// <summary>Gets or sets whether audio enhancement settings can be applied.</summary>
    public bool IsAudioEnhancementsSetSupported { get; set; } = false;

    /// <summary>Gets or sets whether voice processing settings can be read.</summary>
    public bool IsVoiceProcessingReadSupported { get; set; } = false;

    /// <summary>Gets or sets whether voice processing settings can be applied.</summary>
    public bool IsVoiceProcessingSetSupported { get; set; } = false;
}