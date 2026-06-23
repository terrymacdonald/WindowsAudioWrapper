namespace WindowsAudioWrapper.Providers;

using System;
using WindowsAudioWrapper.Models;

internal sealed class SpatialSoundProvider : ISpatialSoundProvider
{
    public SpatialSoundProfile GetSpatialSound(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        return new SpatialSoundProfile
        {
            Mode = SpatialSoundMode.Off,
            FormatId = string.Empty,
            DisplayName = "Off"
        };
    }

    public void SetSpatialSound(AudioEndpointReference endpoint, SpatialSoundProfile spatialSound)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(spatialSound);
        throw new NotSupportedException("Spatial sound setting is not supported by this version of WindowsAudioWrapper.");
    }
}