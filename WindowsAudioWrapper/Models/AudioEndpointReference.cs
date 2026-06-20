using System.Text.Json.Serialization;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// Acts as a data-contract container tracking unmanaged endpoint identities.
/// Cleaned up to explicitly separate schema configuration profiles from internal COM tracking telemetry.
/// </summary>
public class AudioEndpointReference
{
    /// <summary>Gets or sets the absolute unmanaged IMMDevice tracking pointer token string.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the physical PnP hardware container tracking guid identifier.</summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the user presentation display name context string.</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the multi-tier driver descriptor block entries.</summary>
    public HardwareDetails HardwareDetails { get; set; } = new();

    /// <summary>Gets or sets the full structural endpoint descriptor naming string. Ignored in JSON configuration files.</summary>
    [JsonIgnore]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the underlying system audio flow context mapping. Ignored in JSON configuration files.</summary>
    [JsonIgnore]
    public AudioFlow Flow { get; set; }

    /// <summary>Gets or sets a value indicating whether this endpoint reference layer is valid. Ignored in JSON configuration files.</summary>
    [JsonIgnore]
    public bool IsEndpointEnabled { get; set; }

    /// <summary>
    /// Translates unmanaged live endpoint structures cleanly down to serializable contract configurations.
    /// </summary>
    /// <param name="info">The unmanaged system property information snapshot container source.</param>
    /// <returns>A target profile mapping ready for local storage or validation runs.</returns>
    public static AudioEndpointReference FromEndpointInfo(AudioEndpointInfo info)
    {
        if (info == null)
        {
            return new AudioEndpointReference();
        }

        return new AudioEndpointReference
        {
            DeviceId = info.DeviceId,
            ContainerId = info.ContainerId,
            FriendlyName = info.FriendlyName,
            FullName = info.FullName,
            Flow = info.Flow,
            IsEndpointEnabled = info.State.HasFlag(AudioDeviceState.Active),
            HardwareDetails = new HardwareDetails
            {
                DeviceDescription = info.HardwareDetails?.DeviceDescription ?? string.Empty,
                HardwareId = info.HardwareDetails?.HardwareId ?? string.Empty,
                DriverVersion = info.HardwareDetails?.DriverVersion ?? string.Empty,
                EndpointAssociationGuid = info.HardwareDetails?.EndpointAssociationGuid ?? string.Empty
            }
        };
    }
}