namespace WindowsAudioWrapper.Tests;

using WindowsAudioWrapper.Models;
using WindowsAudioWrapper.Providers;

/// <summary>In-memory providers and native setter substitutes; never enumerates or changes Windows audio.</summary>
internal sealed class EndpointTestProviders : IAudioDeviceProvider, IDefaultAudioDeviceProvider, IAudioVolumeProvider,
    IAudioFormatProvider, IAudioEnhancementProvider, ISystemAudioProvider, IDisposable
{
    internal List<AudioEndpointInfo> Devices { get; } = new();
    internal List<(string Setting, string Id)> Writes { get; } = new();
    internal Func<IReadOnlyList<AudioEndpointInfo>>? ReadInventory { get; set; }
    internal bool Disposed { get; private set; }
    internal int RegistrationAttempts { get; private set; }
    internal bool RegistrationSucceeds { get; set; }
    internal bool ThrowOnVolume { get; set; }
    internal WindowsAudioController Controller() => new(this, this, this, this, this, this,
        () => { RegistrationAttempts++; return RegistrationSucceeds; },
        (id, _) => Writes.Add(("visibility", id)), (id, _, _) => Writes.Add(("channels", id)),
        (id, _) => Writes.Add(("properties", id)));
    private IReadOnlyList<AudioEndpointInfo> Inventory()
    {
        if (Disposed) throw new InvalidOperationException("Provider read after disposal.");
        return ReadInventory?.Invoke() ?? Devices.ToArray();
    }
    public IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states) => Inventory().Where(device => device.Flow == AudioFlow.Render && (device.State & states) != 0).ToArray();
    public IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states) => Inventory().Where(device => device.Flow == AudioFlow.Capture && (device.State & states) != 0).ToArray();
    public AudioEndpointInfo ResolveEndpoint(AudioEndpointReference endpoint, AudioFlow flow) => AudioEndpointResolver.Resolve(endpoint, flow, Inventory());
    private static AudioEndpointInfo Missing() => new() { State = AudioDeviceState.NotPresent };
    public AudioEndpointInfo GetDefaultPlaybackDevice() => Missing();
    public AudioEndpointInfo GetDefaultConsolePlaybackDevice() => Missing();
    public AudioEndpointInfo GetDefaultRecordingDevice() => Missing();
    public AudioEndpointInfo GetDefaultConsoleRecordingDevice() => Missing();
    public AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice() => Missing();
    public AudioEndpointInfo GetDefaultCommunicationsRecordingDevice() => Missing();
    public void SetDefaultPlaybackDevice(AudioEndpointReference endpoint) => Writes.Add(("playback", endpoint.DeviceId));
    public void SetDefaultConsolePlaybackDevice(AudioEndpointReference endpoint) => Writes.Add(("console-playback", endpoint.DeviceId));
    public void SetDefaultRecordingDevice(AudioEndpointReference endpoint) => Writes.Add(("recording", endpoint.DeviceId));
    public void SetDefaultConsoleRecordingDevice(AudioEndpointReference endpoint) => Writes.Add(("console-recording", endpoint.DeviceId));
    public void SetDefaultCommunicationsPlaybackDevice(AudioEndpointReference endpoint) => Writes.Add(("communications-playback", endpoint.DeviceId));
    public void SetDefaultCommunicationsRecordingDevice(AudioEndpointReference endpoint) => Writes.Add(("communications-recording", endpoint.DeviceId));
    public decimal GetVolumePercent(AudioEndpointReference endpoint) => 50;
    public void SetVolumePercent(AudioEndpointReference endpoint, decimal value)
    { if (ThrowOnVolume) throw new IOException("Synthetic volume error."); Writes.Add(("volume", endpoint.DeviceId)); }
    public bool GetMute(AudioEndpointReference endpoint) => false;
    public void SetMute(AudioEndpointReference endpoint, bool value) => Writes.Add(("mute", endpoint.DeviceId));
    public AudioFormatProfile GetFormat(string deviceId) => new();
    public void SetFormat(AudioEndpointReference endpoint, AudioFormatProfile value) => Writes.Add(("format", endpoint.DeviceId));
    public AudioEnhancementProfile GetAudioEnhancements(string deviceId) => new();
    public void SetAudioEnhancements(AudioEndpointReference endpoint, AudioEnhancementProfile value) => Writes.Add(("enhancements", endpoint.DeviceId));
    public bool IsMonoAudioReadSupported => false;
    public bool IsMonoAudioSetSupported => false;
    public bool GetMonoAudio() => false;
    public void SetMonoAudio(bool value) { }
    public void Dispose() => Disposed = true;
    internal static AudioEndpointInfo Device(string id, AudioFlow flow = AudioFlow.Render, string container = "container", string name = "Speaker") => new()
    {
        DeviceId = id, Flow = flow, ContainerId = container, FriendlyName = name, FullName = name + " full",
        State = AudioDeviceState.Active, Capabilities = new() { IsVolumeSupported = true, IsMuteSupported = true }
    };
}
