namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents voice processing enhancements typically applied to capture devices.
/// </summary>
public sealed class VoiceProcessingProfile
{
    /// <summary>Gets or sets whether Voice Focus is enabled.</summary>
    public bool IsVoiceFocusEnabled { get; set; } = false;

    /// <summary>Gets or sets whether Noise Suppression is enabled.</summary>
    public bool IsNoiseSuppressionEnabled { get; set; } = false;

    /// <summary>Gets or sets whether Acoustic Echo Cancellation (AEC) is enabled.</summary>
    public bool IsAcousticEchoCancellationEnabled { get; set; } = false;

    /// <summary>Gets or sets whether Automatic Gain Control (AGC) is enabled.</summary>
    public bool IsAutomaticGainControlEnabled { get; set; } = false;
}