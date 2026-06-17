namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal sealed class AudioEnhancementProvider : IAudioEnhancementProvider
{
    public AudioEnhancementProfile GetAudioEnhancements(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        return new AudioEnhancementProfile();
    }

    public void SetAudioEnhancements(AudioEndpointReference endpoint, AudioEnhancementProfile audioEnhancements)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(audioEnhancements);
        audioEnhancements.EnsureDefaults();
        throw new NotSupportedException("Audio enhancement setting is not supported by this version of WindowsAudioWrapper.");
    }

    public VoiceProcessingProfile GetVoiceProcessing(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        return new VoiceProcessingProfile();
    }

    public void SetVoiceProcessing(AudioEndpointReference endpoint, VoiceProcessingProfile voiceProcessing)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(voiceProcessing);
        throw new NotSupportedException("Voice processing setting is not supported by this version of WindowsAudioWrapper.");
    }
}
