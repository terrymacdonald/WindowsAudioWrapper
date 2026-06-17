namespace WindowsAudioWrapper.Models;

public sealed class AudioEndpointCapabilities
{
    public bool IsDefaultDeviceSupported { get; set; } = true;

    public bool IsDefaultCommunicationsDeviceSupported { get; set; } = true;

    public bool IsVolumeSupported { get; set; } = true;

    public bool IsMuteSupported { get; set; } = true;

    public bool IsFormatReadSupported { get; set; } = false;

    public bool IsFormatSetSupported { get; set; } = false;

    public bool IsSpatialSoundReadSupported { get; set; } = false;

    public bool IsSpatialSoundSetSupported { get; set; } = false;

    public bool IsAudioEnhancementsReadSupported { get; set; } = false;

    public bool IsAudioEnhancementsSetSupported { get; set; } = false;

    public bool IsVoiceProcessingReadSupported { get; set; } = false;

    public bool IsVoiceProcessingSetSupported { get; set; } = false;
}
