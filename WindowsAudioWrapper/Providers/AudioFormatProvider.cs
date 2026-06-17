namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

/// <summary>
/// Placeholder provider for shared/exclusive audio format read and set operations.
/// </summary>
internal sealed class AudioFormatProvider : IAudioFormatProvider
{
    public AudioFormatProfile GetFormat(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        throw new NotImplementedException("Audio format read has not been implemented yet.");
    }

    public void SetFormat(AudioEndpointReference endpoint, AudioFormatProfile format)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(format);
        throw new NotImplementedException("Audio format set has not been implemented yet.");
    }
}
