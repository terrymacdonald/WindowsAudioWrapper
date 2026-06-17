namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal interface IAudioFormatProvider
{
    AudioFormatProfile GetFormat(string deviceId);

    void SetFormat(AudioEndpointReference endpoint, AudioFormatProfile format);
}
