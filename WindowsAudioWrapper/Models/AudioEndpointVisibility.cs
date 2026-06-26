namespace WindowsAudioWrapper.Models;

/// <summary>
/// Captures the persisted visibility state for a Windows audio endpoint.
/// </summary>
public sealed class AudioEndpointVisibility
{
    /// <summary>Gets or sets the endpoint identifier used by Windows MMDevice APIs.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint container identifier used as a fallback resolver.</summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint friendly name used as a human-readable fallback resolver.</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the endpoint is playback or recording.</summary>
    public AudioFlow Flow { get; set; } = AudioFlow.Unknown;

    /// <summary>Gets or sets whether the endpoint should be disabled/hidden in Windows.</summary>
    public bool IsDisabled { get; set; }

    /// <summary>Creates a serializable visibility record from a live endpoint.</summary>
    public static AudioEndpointVisibility FromEndpointInfo(AudioEndpointInfo endpoint)
    {
        if (endpoint == null)
        {
            return new AudioEndpointVisibility();
        }

        return new AudioEndpointVisibility
        {
            DeviceId = endpoint.DeviceId,
            ContainerId = endpoint.ContainerId,
            FriendlyName = endpoint.FriendlyName,
            Flow = endpoint.Flow,
            IsDisabled = endpoint.State.HasFlag(AudioDeviceState.Disabled)
        };
    }
}
