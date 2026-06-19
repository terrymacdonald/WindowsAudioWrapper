namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;

internal sealed class AudioVolumeProvider : IAudioVolumeProvider
{
    public decimal GetVolumePercent(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ValidateDeviceId(endpoint);

        IAudioEndpointVolume volume = CoreAudioUtilities.ActivateEndpointVolume(endpoint.DeviceId);
        volume.GetMasterVolumeLevelScalar(out float scalar);
        return Math.Round((decimal)scalar * 100m, 2);
    }

    public void SetVolumePercent(AudioEndpointReference endpoint, decimal volumePercent)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThan(volumePercent, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volumePercent, 100);
        ValidateDeviceId(endpoint);

        IAudioEndpointVolume volume = CoreAudioUtilities.ActivateEndpointVolume(endpoint.DeviceId);
        float scalar = (float)(volumePercent / 100m);
        Guid eventContext = Guid.Empty;
        volume.SetMasterVolumeLevelScalar(scalar, in eventContext);
    }

    public bool GetMute(AudioEndpointReference endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ValidateDeviceId(endpoint);

        IAudioEndpointVolume volume = CoreAudioUtilities.ActivateEndpointVolume(endpoint.DeviceId);
        volume.GetMute(out bool muted);
        return muted;
    }

    public void SetMute(AudioEndpointReference endpoint, bool muted)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ValidateDeviceId(endpoint);

        IAudioEndpointVolume volume = CoreAudioUtilities.ActivateEndpointVolume(endpoint.DeviceId);
        Guid eventContext = Guid.Empty;
        volume.SetMute(muted, in eventContext);
    }

    private static void ValidateDeviceId(AudioEndpointReference endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
        {
            throw new ArgumentException("Endpoint DeviceId is required for volume or mute operations.", nameof(endpoint));
        }
    }
}