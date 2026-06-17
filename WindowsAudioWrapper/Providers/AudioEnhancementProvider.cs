namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

/// <summary>
/// Placeholder provider for audio enhancements and capture voice processing features.
/// </summary>
internal sealed class AudioEnhancementProvider : IAudioEnhancementProvider
{
    public AudioEnhancementProfile GetAudioEnhancements(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        throw new NotImplementedException("Audio enhancement read has not been implemented yet.");
    }

    public void SetAudioEnhancements(AudioEndpointReference endpoint, AudioEnhancementProfile audioEnhancements)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(audioEnhancements);
        audioEnhancements.EnsureDefaults();
        throw new NotImplementedException("Audio enhancement set has not been implemented yet.");
    }

    public VoiceProcessingProfile GetVoiceProcessing(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        throw new NotImplementedException("Voice processing read has not been implemented yet.");
    }

    public void SetVoiceProcessing(AudioEndpointReference endpoint, VoiceProcessingProfile voiceProcessing)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(voiceProcessing);
        throw new NotImplementedException("Voice processing set has not been implemented yet.");
    }
}
