namespace WindowsAudioWrapper.Models;

public class HardwareDetails
{
    public string DeviceDescription { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string DriverVersion { get; set; } = string.Empty;
    public string EndpointAssociationGuid { get; set; } = string.Empty;
}