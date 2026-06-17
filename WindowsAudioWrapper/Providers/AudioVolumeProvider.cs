namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

/// <summary>
/// Placeholder provider for IAudioEndpointVolume operations.
/// </summary>
internal sealed class AudioVolumeProvider : IAudioVolumeProvider
{
    public decimal GetVolumePercent(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        throw new NotImplementedException("Endpoint volume read has not been implemented yet.");
    }

    public void SetVolumePercent(AudioEndpointReference endpoint, decimal volumePercent)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThan(volumePercent, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volumePercent, 100);
        throw new NotImplementedException("Endpoint volume set has not been implemented yet.");
    }

    public bool GetMute(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        throw new NotImplementedException("Endpoint mute read has not been implemented yet.");
    }

    public void SetMute(AudioEndpointReference endpoint, bool muted)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        throw new NotImplementedException("Endpoint mute set has not been implemented yet.");
    }
}
