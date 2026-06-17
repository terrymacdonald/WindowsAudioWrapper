namespace WindowsAudioWrapper.Models;

public sealed class AudioEndpointReference
{
    public string DeviceId { get; set; } = string.Empty;

    public string ContainerId { get; set; } = string.Empty;

    public string FriendlyName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public AudioFlow Flow { get; set; } = AudioFlow.Unknown;

    public bool IsEndpointEnabled =>
        !string.IsNullOrWhiteSpace(DeviceId) ||
        !string.IsNullOrWhiteSpace(FullName) ||
        !string.IsNullOrWhiteSpace(FriendlyName);

    public static AudioEndpointReference FromEndpointInfo(AudioEndpointInfo endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        return new AudioEndpointReference
        {
            DeviceId = endpoint.DeviceId,
            ContainerId = endpoint.ContainerId,
            FriendlyName = endpoint.FriendlyName,
            FullName = endpoint.FullName,
            Flow = endpoint.Flow
        };
    }
}
