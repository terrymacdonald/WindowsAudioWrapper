namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal interface IAudioEnhancementProvider
{
    AudioEnhancementProfile GetAudioEnhancements(string deviceId);
    void SetAudioEnhancements(AudioEndpointReference endpoint, AudioEnhancementProfile audioEnhancements);
}