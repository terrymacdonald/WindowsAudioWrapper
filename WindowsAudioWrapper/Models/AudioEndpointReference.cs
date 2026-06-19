namespace WindowsAudioWrapper.Models;

/// <summary>
/// A lightweight reference used to target a specific audio device when applying a profile.
/// </summary>
public sealed class AudioEndpointReference
{
    /// <summary>Gets or sets the unique hardware identifier for the device.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the container identifier for the device.</summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the friendly name of the device.</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the full display name of the device.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the expected flow (Render or Capture) of the device.</summary>
    public AudioFlow Flow { get; set; } = AudioFlow.Unknown;

    /// <summary>Gets a value indicating whether this reference has enough data to potentially match a device.</summary>
    public bool IsEndpointEnabled =>
        !string.IsNullOrWhiteSpace(DeviceId) ||
        !string.IsNullOrWhiteSpace(FullName) ||
        !string.IsNullOrWhiteSpace(FriendlyName);

    /// <summary>
    /// Creates a lightweight reference from a full AudioEndpointInfo object.
    /// </summary>
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