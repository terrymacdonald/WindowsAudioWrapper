namespace WindowsAudioWrapper.Models;

/// <summary>Indicates which settings can be read or changed for an audio endpoint.</summary>
public sealed class AudioEndpointCapabilities
{
    /// <summary>Gets or sets whether the default multimedia device can be changed.</summary>
    public bool IsDefaultDeviceSupported { get; set; } = true;
    /// <summary>Gets or sets whether the default communications device can be changed.</summary>
    public bool IsDefaultCommunicationsDeviceSupported { get; set; } = true;
    /// <summary>Gets or sets whether endpoint volume can be changed.</summary>
    public bool IsVolumeSupported { get; set; } = true;
    /// <summary>Gets or sets whether endpoint mute can be changed.</summary>
    public bool IsMuteSupported { get; set; } = true;
    /// <summary>Gets or sets whether the endpoint format can be read.</summary>
    public bool IsFormatReadSupported { get; set; } = false;
    /// <summary>Gets or sets whether the endpoint format can be changed.</summary>
    public bool IsFormatSetSupported { get; set; } = false;
    /// <summary>Gets or sets whether audio enhancement settings can be read.</summary>
    public bool IsAudioEnhancementsReadSupported { get; set; } = false;
    /// <summary>Gets or sets whether audio enhancement settings can be changed.</summary>
    public bool IsAudioEnhancementsSetSupported { get; set; } = false;
}
