namespace WindowsAudioWrapper.Providers;

using WindowsAudioWrapper.Models;

/// <summary>Selects an endpoint consistently without touching Windows or mutating saved criteria.</summary>
internal static class AudioEndpointResolver
{
    /// <summary>Resolves exact identity first, then unique fallback identity within the requested direction.</summary>
    internal static AudioEndpointInfo Resolve(AudioEndpointReference endpoint, AudioFlow expectedFlow, IEnumerable<AudioEndpointInfo> inventory)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (expectedFlow != AudioFlow.Unknown && endpoint.Flow != AudioFlow.Unknown && endpoint.Flow != expectedFlow)
            return Missing(endpoint, expectedFlow);
        AudioFlow flow = expectedFlow == AudioFlow.Unknown ? endpoint.Flow : expectedFlow;
        AudioEndpointInfo[] devices = inventory.Where(device => flow == AudioFlow.Unknown || device.Flow == flow).ToArray();
        foreach (Func<AudioEndpointInfo, bool>? matches in new Func<AudioEndpointInfo, bool>?[]
        {
            string.IsNullOrWhiteSpace(endpoint.DeviceId) ? null : device => Same(device.DeviceId, endpoint.DeviceId),
            string.IsNullOrWhiteSpace(endpoint.ContainerId) ? null : device => Same(device.ContainerId, endpoint.ContainerId),
            string.IsNullOrWhiteSpace(endpoint.FullName) ? null : device => Same(device.FullName, endpoint.FullName),
            string.IsNullOrWhiteSpace(endpoint.FriendlyName) ? null : device => Same(device.FriendlyName, endpoint.FriendlyName)
        })
        {
            if (matches == null) continue;
            AudioEndpointInfo[] candidates = devices.Where(matches).ToArray();
            if (candidates.Length == 1) return candidates[0];
            if (candidates.Length > 1)
                throw new AmbiguousAudioEndpointException("Multiple audio endpoints match the saved reference; no endpoint was selected.");
        }
        return Missing(endpoint, flow);
    }

    /// <summary>Compares endpoint identifiers without culture-sensitive casing.</summary>
    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    /// <summary>Creates a non-present result retaining the requested identity for diagnostics.</summary>
    internal static AudioEndpointInfo Missing(AudioEndpointReference endpoint, AudioFlow flow) => new()
    {
        DeviceId = endpoint.DeviceId ?? string.Empty, ContainerId = endpoint.ContainerId ?? string.Empty,
        FriendlyName = endpoint.FriendlyName ?? string.Empty, FullName = endpoint.FullName ?? string.Empty,
        Flow = flow, State = AudioDeviceState.NotPresent,
        Capabilities = new AudioEndpointCapabilities
        {
            IsDefaultDeviceSupported = false, IsDefaultCommunicationsDeviceSupported = false,
            IsVolumeSupported = false, IsMuteSupported = false,
            IsAudioEnhancementsReadSupported = false, IsAudioEnhancementsSetSupported = false
        }
    };
}

/// <summary>Identifies an unsafe ambiguous fallback without exposing a new public exception contract.</summary>
internal sealed class AmbiguousAudioEndpointException(string message) : WindowsAudioWrapperException(message);
