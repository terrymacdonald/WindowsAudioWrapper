namespace WindowsAudioWrapper.Models;

public sealed class AudioEndpointInfo
{
    public string DeviceId { get; set; } = string.Empty;

    public string ContainerId { get; set; } = string.Empty;

    public string FriendlyName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public AudioFlow Flow { get; set; } = AudioFlow.Unknown;

    public AudioDeviceState State { get; set; } = AudioDeviceState.Unknown;

    public bool IsDefaultDevice { get; set; } = false;

    public bool IsDefaultCommunicationsDevice { get; set; } = false;

    public decimal VolumePercent { get; set; } = 50;

    public bool IsMuted { get; set; } = false;

    public AudioEndpointCapabilities Capabilities { get; set; } = new();

    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(DeviceId) &&
        State.HasFlag(AudioDeviceState.Active);

    public void EnsureDefaults()
    {
        Capabilities ??= new AudioEndpointCapabilities();
    }
}
