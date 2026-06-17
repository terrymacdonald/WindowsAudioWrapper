namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal interface IAudioVolumeProvider
{
    decimal GetVolumePercent(AudioEndpointReference endpoint);

    void SetVolumePercent(AudioEndpointReference endpoint, decimal volumePercent);

    bool GetMute(AudioEndpointReference endpoint);

    void SetMute(AudioEndpointReference endpoint, bool muted);
}
