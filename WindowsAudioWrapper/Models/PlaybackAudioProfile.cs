namespace WindowsAudioWrapper.Models;

public sealed class PlaybackAudioProfile
{
    public bool IsPlaybackEnabled { get; set; } = false;

    public AudioEndpointReference Device { get; set; } = new();

    public AudioEndpointReference CommunicationsDevice { get; set; } = new();

    public bool IsDefaultPlaybackDeviceEnabled { get; set; } = false;

    public bool IsDefaultCommunicationsPlaybackDeviceEnabled { get; set; } = false;

    public bool IsVolumeEnabled { get; set; } = false;

    public decimal VolumePercent { get; set; } = 50;

    public bool IsMuteEnabled { get; set; } = false;

    public bool IsMuted { get; set; } = false;

    public bool IsFormatEnabled { get; set; } = false;

    public AudioFormatProfile Format { get; set; } = new();

    public bool IsSpatialSoundEnabled { get; set; } = false;

    public SpatialSoundProfile SpatialSound { get; set; } = new();

    public bool IsAudioEnhancementsEnabled { get; set; } = false;

    public AudioEnhancementProfile AudioEnhancements { get; set; } = new();

    public void EnsureDefaults()
    {
        Device ??= new AudioEndpointReference();
        CommunicationsDevice ??= new AudioEndpointReference();
        Format ??= new AudioFormatProfile();
        SpatialSound ??= new SpatialSoundProfile();
        AudioEnhancements ??= new AudioEnhancementProfile();
    }
}
