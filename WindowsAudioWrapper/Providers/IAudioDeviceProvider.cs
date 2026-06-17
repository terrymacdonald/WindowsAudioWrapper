namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal interface IAudioDeviceProvider
{
    IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states);

    IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states);

    AudioEndpointInfo ResolveEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow);
}
