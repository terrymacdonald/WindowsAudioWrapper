using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class DeviceEnumerationTests
{
    [SkippableFact]
    public void GetPlaybackDevices_ShouldReturnRealPlaybackEndpoints_WhenSupported()
    {
        using WindowsAudioController controller = new();

        IReadOnlyList<AudioEndpointInfo> devices = HardwareTestHelpers.RunOrSkip(
            () => controller.GetPlaybackDevices(AudioDeviceState.Active | AudioDeviceState.Unplugged),
            "playback device enumeration");

        HardwareTestHelpers.SkipIfNoDevices(devices, "playback");

        Assert.All(devices, device =>
        {
            Assert.NotNull(device);
            device.EnsureDefaults();
            Assert.True(device.Flow is AudioFlow.Render or AudioFlow.Unknown, $"Expected Render flow but found {device.Flow} for {device.FriendlyName}.");
            Assert.False(string.IsNullOrWhiteSpace(device.DeviceId), "Playback devices should include a stable Windows endpoint DeviceId.");
        });
    }

    [SkippableFact]
    public void GetRecordingDevices_ShouldReturnRealRecordingEndpoints_WhenSupported()
    {
        using WindowsAudioController controller = new();

        IReadOnlyList<AudioEndpointInfo> devices = HardwareTestHelpers.RunOrSkip(
            () => controller.GetRecordingDevices(AudioDeviceState.Active | AudioDeviceState.Unplugged),
            "recording device enumeration");

        HardwareTestHelpers.SkipIfNoDevices(devices, "recording");

        Assert.All(devices, device =>
        {
            Assert.NotNull(device);
            device.EnsureDefaults();
            Assert.True(device.Flow is AudioFlow.Capture or AudioFlow.Unknown, $"Expected Capture flow but found {device.Flow} for {device.FriendlyName}.");
            Assert.False(string.IsNullOrWhiteSpace(device.DeviceId), "Recording devices should include a stable Windows endpoint DeviceId.");
        });
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldReturnActiveRenderEndpoint_WhenSupported()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo endpoint = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultPlaybackDevice,
            "default playback device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(endpoint, "playback");

        Assert.Equal(AudioFlow.Render, endpoint.Flow);
        Assert.True(endpoint.IsDefaultDevice);
        Assert.False(string.IsNullOrWhiteSpace(endpoint.DeviceId));
    }

    [SkippableFact]
    public void GetDefaultRecordingDevice_ShouldReturnActiveCaptureEndpoint_WhenSupported()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo endpoint = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultRecordingDevice,
            "default recording device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(endpoint, "recording");

        Assert.Equal(AudioFlow.Capture, endpoint.Flow);
        Assert.True(endpoint.IsDefaultDevice);
        Assert.False(string.IsNullOrWhiteSpace(endpoint.DeviceId));
    }

    [SkippableFact]
    public void GetDefaultCommunicationsPlaybackDevice_ShouldReturnActiveRenderEndpoint_WhenSupported()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo endpoint = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultCommunicationsPlaybackDevice,
            "default communications playback device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(endpoint, "communications playback");

        Assert.Equal(AudioFlow.Render, endpoint.Flow);
        Assert.True(endpoint.IsDefaultCommunicationsDevice);
        Assert.False(string.IsNullOrWhiteSpace(endpoint.DeviceId));
    }

    [SkippableFact]
    public void GetDefaultCommunicationsRecordingDevice_ShouldReturnActiveCaptureEndpoint_WhenSupported()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo endpoint = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultCommunicationsRecordingDevice,
            "default communications recording device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(endpoint, "communications recording");

        Assert.Equal(AudioFlow.Capture, endpoint.Flow);
        Assert.True(endpoint.IsDefaultCommunicationsDevice);
        Assert.False(string.IsNullOrWhiteSpace(endpoint.DeviceId));
    }
}
