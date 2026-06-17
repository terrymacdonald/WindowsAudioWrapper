namespace WindowsAudioWrapper.Models;

public sealed class VoiceProcessingProfile
{
    public bool IsVoiceFocusEnabled { get; set; } = false;

    public bool IsNoiseSuppressionEnabled { get; set; } = false;

    public bool IsAcousticEchoCancellationEnabled { get; set; } = false;

    public bool IsAutomaticGainControlEnabled { get; set; } = false;
}
