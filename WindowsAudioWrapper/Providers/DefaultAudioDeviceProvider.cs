namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

/// <summary>
/// Placeholder provider for reading and setting default audio endpoints.
/// Setting default endpoints will likely use PolicyConfig interop internally.
/// </summary>
internal sealed class DefaultAudioDeviceProvider : IDefaultAudioDeviceProvider
{
    public AudioEndpointInfo GetDefaultPlaybackDevice()
    {
        throw new NotImplementedException("Default playback device read has not been implemented yet.");
    }

    public AudioEndpointInfo GetDefaultRecordingDevice()
    {
        throw new NotImplementedException("Default recording device read has not been implemented yet.");
    }

    public AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice()
    {
        throw new NotImplementedException("Default communications playback device read has not been implemented yet.");
    }

    public AudioEndpointInfo GetDefaultCommunicationsRecordingDevice()
    {
        throw new NotImplementedException("Default communications recording device read has not been implemented yet.");
    }

    public void SetDefaultPlaybackDevice(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        throw new NotImplementedException("Default playback device set has not been implemented yet.");
    }

    public void SetDefaultRecordingDevice(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        throw new NotImplementedException("Default recording device set has not been implemented yet.");
    }

    public void SetDefaultCommunicationsPlaybackDevice(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        throw new NotImplementedException("Default communications playback device set has not been implemented yet.");
    }

    public void SetDefaultCommunicationsRecordingDevice(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        throw new NotImplementedException("Default communications recording device set has not been implemented yet.");
    }
}
