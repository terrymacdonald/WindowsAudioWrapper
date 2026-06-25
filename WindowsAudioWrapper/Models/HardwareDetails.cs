namespace WindowsAudioWrapper.Models;

/// <summary>
/// Contains extended, low-level unmanaged property telemetry mapping for hardware endpoints.
/// Strictly respects zero-null constraints for integration stability.
/// </summary>
public class HardwareDetails
{
    public string DeviceDescription { get; set; } = string.Empty;
    public int FormFactorCode { get; set; } = 0;
    public uint PhysicalSpeakersMask { get; set; } = 0;
    public uint FullRangeSpeakersMask { get; set; } = 0;
    public string EndpointGuid { get; set; } = string.Empty;
    public string DeviceFormatSummary { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the endpoint supports WASAPI low-latency event-driven buffer loops.</summary>
    public bool SupportsEventDrivenMode { get; set; } = false;

    /// <summary>Gets or sets the string representation of the connection type (e.g., Analog35mm, HDMI, Bluetooth).</summary>
    public string JackSubType { get; set; } = string.Empty;

}
