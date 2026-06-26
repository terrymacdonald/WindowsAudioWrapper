using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class DefaultDeviceRoleTests
{
    [SkippableFact]
    public void GetCurrentProfile_ShouldCaptureConsolePlaybackDevice()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo consolePlayback = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultConsolePlaybackDevice,
            "default console playback device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(consolePlayback, "default console playback");

        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        Assert.True(profile.Playback.IsDefaultConsolePlaybackDeviceEnabled);
        Assert.Equal(consolePlayback.DeviceId, profile.Playback.ConsoleDevice.DeviceId, ignoreCase: true);
    }

    [SkippableFact]
    public void GetCurrentProfile_ShouldCaptureConsoleRecordingDevice()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo consoleRecording = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultConsoleRecordingDevice,
            "default console recording device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(consoleRecording, "default console recording");

        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        Assert.True(profile.Recording.IsDefaultConsoleRecordingDeviceEnabled);
        Assert.Equal(consoleRecording.DeviceId, profile.Recording.ConsoleDevice.DeviceId, ignoreCase: true);
    }

    [SkippableFact]
    public void GetCurrentProfile_ShouldCaptureEndpointVisibilities()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        profile.EnsureDefaults();

        Assert.True(profile.IsEndpointVisibilityTrackingEnabled);
        Assert.NotEmpty(profile.EndpointVisibilities);

        Assert.All(profile.EndpointVisibilities, endpoint =>
        {
            Assert.False(string.IsNullOrWhiteSpace(endpoint.DeviceId));
            Assert.True(endpoint.Flow is AudioFlow.Render or AudioFlow.Capture);
        });

        if (profile.Playback.MultimediaDevice.IsEndpointEnabled)
        {
            AudioEndpointVisibility playbackVisibility = Assert.Single(
                profile.EndpointVisibilities,
                endpoint => endpoint.DeviceId.Equals(profile.Playback.MultimediaDevice.DeviceId, StringComparison.OrdinalIgnoreCase));
            Assert.False(playbackVisibility.IsDisabled);
        }
    }
}
