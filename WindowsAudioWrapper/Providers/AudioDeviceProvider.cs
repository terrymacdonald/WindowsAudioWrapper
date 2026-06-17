namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

/// <summary>
/// Placeholder provider for MMDevice enumeration and endpoint resolution.
/// Replace NotImplementedException bodies with Core Audio COM implementation.
/// </summary>
internal sealed class AudioDeviceProvider : IAudioDeviceProvider
{
    public IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states)
    {
        throw new NotImplementedException("Core Audio playback enumeration has not been implemented yet.");
    }

    public IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states)
    {
        throw new NotImplementedException("Core Audio recording enumeration has not been implemented yet.");
    }

    public AudioEndpointInfo ResolveEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        IReadOnlyList<AudioEndpointInfo> devices = expectedFlow switch
        {
            AudioFlow.Render => GetPlaybackDevices(AudioDeviceState.All),
            AudioFlow.Capture => GetRecordingDevices(AudioDeviceState.All),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedFlow), expectedFlow, "Expected Render or Capture.")
        };

        AudioEndpointInfo? match = null;

        if (!string.IsNullOrWhiteSpace(endpoint.DeviceId))
        {
            match = devices.FirstOrDefault(device =>
                device.DeviceId.Equals(endpoint.DeviceId, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null && !string.IsNullOrWhiteSpace(endpoint.FullName))
        {
            match = devices.FirstOrDefault(device =>
                device.FullName.Equals(endpoint.FullName, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null && !string.IsNullOrWhiteSpace(endpoint.FriendlyName))
        {
            match = devices.FirstOrDefault(device =>
                device.FriendlyName.Equals(endpoint.FriendlyName, StringComparison.OrdinalIgnoreCase));
        }

        return match ?? new AudioEndpointInfo
        {
            DeviceId = endpoint.DeviceId,
            ContainerId = endpoint.ContainerId,
            FriendlyName = endpoint.FriendlyName,
            FullName = endpoint.FullName,
            Flow = expectedFlow,
            State = AudioDeviceState.NotPresent
        };
    }
}
