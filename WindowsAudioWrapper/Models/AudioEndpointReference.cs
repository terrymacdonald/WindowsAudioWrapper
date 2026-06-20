namespace WindowsAudioWrapper.Models;

public class AudioEndpointReference
{
    public string DeviceId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public AudioFlow Flow { get; set; }
    public bool IsEndpointEnabled { get; set; }
    public HardwareDetails HardwareDetails { get; set; } = new();

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