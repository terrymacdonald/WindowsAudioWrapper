namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the requested state for playback devices within an AudioProfile.
/// </summary>
public sealed class PlaybackAudioProfile
{
    /// <summary>Gets or sets whether any playback settings should be evaluated and applied.</summary>
    public bool IsPlaybackEnabled { get; set; } = false;

    /// <summary>Gets or sets the reference to the targeted primary playback device.</summary>
    public AudioEndpointReference Device { get; set; } = new();

    /// <summary>Gets or sets the reference to the targeted communications playback device.</summary>
    public AudioEndpointReference CommunicationsDevice { get; set; } = new();

    /// <summary>Gets or sets whether the default multimedia playback device should be changed.</summary>
    public bool IsDefaultPlaybackDeviceEnabled { get; set; } = false;

    /// <summary>Gets or sets whether the default communications playback device should be changed.</summary>
    public bool IsDefaultCommunicationsPlaybackDeviceEnabled { get; set; } = false;

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

    /// <summary>Gets or sets whether the target device's spatial sound settings should be changed.</summary>
    public bool IsSpatialSoundEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired spatial sound profile.</summary>
    public SpatialSoundProfile SpatialSound { get; set; } = new();

    /// <summary>Gets or sets whether the target device's audio enhancements should be changed.</summary>
    public bool IsAudioEnhancementsEnabled { get; set; } = false;

    /// <summary>Gets or sets the desired audio enhancement profile.</summary>
    public AudioEnhancementProfile AudioEnhancements { get; set; } = new();

    /// <summary>Ensures all nested objects are instantiated properly after deserialization.</summary>
    public void EnsureDefaults()
    {
        Device ??= new AudioEndpointReference();
        CommunicationsDevice ??= new AudioEndpointReference();
        Format ??= new AudioFormatProfile();
        SpatialSound ??= new SpatialSoundProfile();
        AudioEnhancements ??= new AudioEnhancementProfile();
    }
}