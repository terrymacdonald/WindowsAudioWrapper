namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

internal interface ISpatialSoundProvider
{
    SpatialSoundProfile GetSpatialSound(string deviceId);

    void SetSpatialSound(AudioEndpointReference endpoint, SpatialSoundProfile spatialSound);
}
