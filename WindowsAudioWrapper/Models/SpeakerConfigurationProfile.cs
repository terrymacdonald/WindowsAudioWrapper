namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the Windows speaker configuration for a playback endpoint.
/// </summary>
public sealed class SpeakerConfigurationProfile
{
    /// <summary>Gets or sets the active speaker channel mask.</summary>
    public uint ChannelMask { get; set; }

    /// <summary>Gets or sets the speakers marked as full-range in Windows speaker setup.</summary>
    public uint FullRangeSpeakersMask { get; set; }

    /// <summary>Gets whether the profile contains a speaker configuration.</summary>
    public bool IsConfigured => ChannelMask != 0;
}
