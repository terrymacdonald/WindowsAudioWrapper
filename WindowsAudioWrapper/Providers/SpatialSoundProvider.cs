namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

/// <summary>
/// Placeholder provider for spatial sound read and set operations.
/// </summary>
internal sealed class SpatialSoundProvider : ISpatialSoundProvider
{
    public SpatialSoundProfile GetSpatialSound(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        throw new NotImplementedException("Spatial sound read has not been implemented yet.");
    }

    public void SetSpatialSound(AudioEndpointReference endpoint, SpatialSoundProfile spatialSound)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(spatialSound);
        throw new NotImplementedException("Spatial sound set has not been implemented yet.");
    }
}
