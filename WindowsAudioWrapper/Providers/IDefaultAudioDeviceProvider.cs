namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal interface IDefaultAudioDeviceProvider
{
    AudioEndpointInfo GetDefaultPlaybackDevice();

    AudioEndpointInfo GetDefaultRecordingDevice();

    AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice();

    AudioEndpointInfo GetDefaultCommunicationsRecordingDevice();

    void SetDefaultPlaybackDevice(AudioEndpointReference endpoint);

    void SetDefaultRecordingDevice(AudioEndpointReference endpoint);

    void SetDefaultCommunicationsPlaybackDevice(AudioEndpointReference endpoint);

    void SetDefaultCommunicationsRecordingDevice(AudioEndpointReference endpoint);
}
