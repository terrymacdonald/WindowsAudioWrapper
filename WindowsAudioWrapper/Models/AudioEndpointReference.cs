using Newtonsoft.Json;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// Acts as a data-contract wrapper tracking hardware location properties used to resolve targets.
/// </summary>
public class AudioEndpointReference
{
    /// <summary>Gets or sets the absolute internal IMMDevice endpoint identifier token.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Plug-and-Play physical container association identifier.</summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized friendly presentation display name of the hardware endpoint.</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the native infrastructure data logs. Safely ignored in JSON layout.</summary>
    [JsonIgnore]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the underlying system audio flow context mapping. Safely ignored in JSON layout.</summary>
    [JsonIgnore]
    public AudioFlow Flow { get; set; }

    /// <summary>Gets or sets a tracking flag stating if the endpoint reference is active. Safely ignored in JSON layout.</summary>
    [JsonIgnore]
    public bool IsEndpointEnabled { get; set; }

    /// <summary>Gets or sets the multi-tier driver descriptor block entries.</summary>
    public HardwareDetails HardwareDetails { get; set; } = new();

    /// <summary>
    /// Translates unmanaged live endpoint structures cleanly down to serializable contract configurations.
    /// </summary>
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
                FormFactorCode = info.HardwareDetails?.FormFactorCode ?? 0,
                PhysicalSpeakersMask = info.HardwareDetails?.PhysicalSpeakersMask ?? 0,
                FullRangeSpeakersMask = info.HardwareDetails?.FullRangeSpeakersMask ?? 0,
                EndpointGuid = info.HardwareDetails?.EndpointGuid ?? string.Empty,
                DeviceFormatSummary = info.HardwareDetails?.DeviceFormatSummary ?? string.Empty,
                SupportsEventDrivenMode = info.HardwareDetails?.SupportsEventDrivenMode ?? false,
                JackSubType = info.HardwareDetails?.JackSubType ?? string.Empty
            }
        };
    }
}
