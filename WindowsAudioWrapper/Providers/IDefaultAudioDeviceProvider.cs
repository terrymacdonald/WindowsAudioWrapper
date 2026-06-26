namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal interface IDefaultAudioDeviceProvider
{
    AudioEndpointInfo GetDefaultPlaybackDevice();

    AudioEndpointInfo GetDefaultConsolePlaybackDevice();

    AudioEndpointInfo GetDefaultRecordingDevice();

    AudioEndpointInfo GetDefaultConsoleRecordingDevice();

    AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice();

    AudioEndpointInfo GetDefaultCommunicationsRecordingDevice();

    void SetDefaultPlaybackDevice(AudioEndpointReference endpoint);

    void SetDefaultConsolePlaybackDevice(AudioEndpointReference endpoint);

    void SetDefaultRecordingDevice(AudioEndpointReference endpoint);

    void SetDefaultConsoleRecordingDevice(AudioEndpointReference endpoint);

    void SetDefaultCommunicationsPlaybackDevice(AudioEndpointReference endpoint);

    void SetDefaultCommunicationsRecordingDevice(AudioEndpointReference endpoint);
}
