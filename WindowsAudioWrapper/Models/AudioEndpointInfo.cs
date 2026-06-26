namespace WindowsAudioWrapper.Models;

/// <summary>
/// Contains detailed information and current state for a specific Windows audio endpoint.
/// </summary>
public sealed class AudioEndpointInfo
{
    /// <summary>Gets or sets the unique hardware identifier for the device.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the container identifier grouping related hardware components.</summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>Gets or sets the friendly name of the device (e.g., "Speakers").</summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the full display name, often including the interface name (e.g., "Speakers (Realtek High Definition Audio)").</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is a playback (Render) or recording (Capture) device.</summary>
    public AudioFlow Flow { get; set; } = AudioFlow.Unknown;

    /// <summary>Gets or sets the current connection and enablement state of the device.</summary>
    public AudioDeviceState State { get; set; } = AudioDeviceState.Unknown;

    /// <summary>Gets or sets a value indicating whether this is the default multimedia device.</summary>
    public bool IsDefaultDevice { get; set; } = false;

    /// <summary>Gets or sets a value indicating whether this is the default console device.</summary>
    public bool IsDefaultConsoleDevice { get; set; } = false;

    /// <summary>Gets or sets a value indicating whether this is the default communications device.</summary>
    public bool IsDefaultCommunicationsDevice { get; set; } = false;

    /// <summary>Gets or sets the current master volume level as a percentage (0-100).</summary>
    public decimal VolumePercent { get; set; } = 50;

    /// <summary>Gets or sets a value indicating whether the device is currently muted.</summary>
    public bool IsMuted { get; set; } = false;

    /// <summary>Gets or sets the capabilities supported by this specific endpoint.</summary>
    public AudioEndpointCapabilities Capabilities { get; set; } = new();

    public HardwareDetails HardwareDetails { get; set; } = new();
    public AudioEnhancementProfile AudioEnhancements { get; set; } = new();

    /// <summary>Gets a value indicating whether the device is present and active.</summary>
    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(DeviceId) &&
        State.HasFlag(AudioDeviceState.Active);

    /// <summary>Ensures all nested objects are instantiated properly after deserialization.</summary>
    public void EnsureDefaults()
    {
        Capabilities ??= new AudioEndpointCapabilities();
    }
}
