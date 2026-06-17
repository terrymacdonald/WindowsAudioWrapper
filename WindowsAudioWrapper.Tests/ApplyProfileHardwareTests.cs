using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class ApplyProfileHardwareTests
{
    [SkippableFact]
    public void ApplyProfile_ShouldChangeAndRestorePlaybackVolume_WhenSupported()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        HardwareTestHelpers.SkipIfPlaybackVolumeUnsupported(originalProfile);

        decimal targetVolume = HardwareTestHelpers.AlternativeVolume(originalProfile.Playback.VolumePercent);
        AudioProfile changedProfile = HardwareTestHelpers.BuildPlaybackVolumeProfile(originalProfile, targetVolume);

        try
        {
            AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(changedProfile),
                "playback volume set");

            HardwareTestHelpers.AssertApplySucceeded(applyResult);

            AudioProfile currentProfile = HardwareTestHelpers.RunOrSkip(
                controller.GetCurrentProfile,
                "current audio profile capture after playback volume set");

            HardwareTestHelpers.SkipIfPlaybackVolumeUnsupported(currentProfile);
            HardwareTestHelpers.AssertVolumeClose(currentProfile.Playback.VolumePercent, targetVolume);
        }
        finally
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(originalProfile),
                "playback volume restore");
        }
    }

    [SkippableFact]
    public void ApplyProfile_ShouldChangeAndRestorePlaybackMute_WhenSupported()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        HardwareTestHelpers.SkipIfPlaybackMuteUnsupported(originalProfile);

        AudioProfile changedProfile = HardwareTestHelpers.BuildPlaybackMuteProfile(
            originalProfile,
            !originalProfile.Playback.IsMuted);

        try
        {
            AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(changedProfile),
                "playback mute set");

            HardwareTestHelpers.AssertApplySucceeded(applyResult);

            AudioProfile currentProfile = HardwareTestHelpers.RunOrSkip(
                controller.GetCurrentProfile,
                "current audio profile capture after playback mute set");

            HardwareTestHelpers.SkipIfPlaybackMuteUnsupported(currentProfile);
            Assert.Equal(changedProfile.Playback.IsMuted, currentProfile.Playback.IsMuted);
        }
        finally
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(originalProfile),
                "playback mute restore");
        }
    }

    [SkippableFact]
    public void ApplyProfile_ShouldChangeAndRestoreRecordingVolume_WhenSupported()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        HardwareTestHelpers.SkipIfRecordingVolumeUnsupported(originalProfile);

        decimal targetVolume = HardwareTestHelpers.AlternativeVolume(originalProfile.Recording.VolumePercent);
        AudioProfile changedProfile = HardwareTestHelpers.BuildRecordingVolumeProfile(originalProfile, targetVolume);

        try
        {
            AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(changedProfile),
                "recording volume set");

            HardwareTestHelpers.AssertApplySucceeded(applyResult);

            AudioProfile currentProfile = HardwareTestHelpers.RunOrSkip(
                controller.GetCurrentProfile,
                "current audio profile capture after recording volume set");

            HardwareTestHelpers.SkipIfRecordingVolumeUnsupported(currentProfile);
            HardwareTestHelpers.AssertVolumeClose(currentProfile.Recording.VolumePercent, targetVolume);
        }
        finally
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(originalProfile),
                "recording volume restore");
        }
    }

    [SkippableFact]
    public void ApplyProfile_ShouldChangeAndRestoreDefaultPlaybackDevice_WhenAtLeastTwoActivePlaybackDevicesExist()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        IReadOnlyList<AudioEndpointInfo> playbackDevices = HardwareTestHelpers.RunOrSkip(
            () => controller.GetPlaybackDevices(AudioDeviceState.Active),
            "active playback device enumeration");

        HardwareTestHelpers.SkipIfNoDevices(playbackDevices, "active playback");
        Skip.If(playbackDevices.Count < 2, "Skipping because changing the default playback device requires at least two active playback devices.");

        AudioEndpointInfo originalDefault = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultPlaybackDevice,
            "default playback device read");

        AudioEndpointInfo targetDevice = playbackDevices.First(device =>
            !device.DeviceId.Equals(originalDefault.DeviceId, StringComparison.OrdinalIgnoreCase));

        AudioProfile changedProfile = HardwareTestHelpers.BuildDefaultPlaybackDeviceProfile(targetDevice);

        try
        {
            AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(changedProfile),
                "default playback device set");

            HardwareTestHelpers.AssertApplySucceeded(applyResult);

            AudioEndpointInfo currentDefault = HardwareTestHelpers.RunOrSkip(
                controller.GetDefaultPlaybackDevice,
                "default playback device read after set");

            Assert.Equal(targetDevice.DeviceId, currentDefault.DeviceId, ignoreCase: true);
        }
        finally
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(originalProfile),
                "default playback device restore");
        }
    }

    [SkippableFact]
    public void ApplyProfile_ShouldChangeAndRestoreDefaultRecordingDevice_WhenAtLeastTwoActiveRecordingDevicesExist()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        IReadOnlyList<AudioEndpointInfo> recordingDevices = HardwareTestHelpers.RunOrSkip(
            () => controller.GetRecordingDevices(AudioDeviceState.Active),
            "active recording device enumeration");

        HardwareTestHelpers.SkipIfNoDevices(recordingDevices, "active recording");
        Skip.If(recordingDevices.Count < 2, "Skipping because changing the default recording device requires at least two active recording devices.");

        AudioEndpointInfo originalDefault = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultRecordingDevice,
            "default recording device read");

        AudioEndpointInfo targetDevice = recordingDevices.First(device =>
            !device.DeviceId.Equals(originalDefault.DeviceId, StringComparison.OrdinalIgnoreCase));

        AudioProfile changedProfile = HardwareTestHelpers.BuildDefaultRecordingDeviceProfile(targetDevice);

        try
        {
            AudioProfileApplyResult applyResult = HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(changedProfile),
                "default recording device set");

            HardwareTestHelpers.AssertApplySucceeded(applyResult);

            AudioEndpointInfo currentDefault = HardwareTestHelpers.RunOrSkip(
                controller.GetDefaultRecordingDevice,
                "default recording device read after set");

            Assert.Equal(targetDevice.DeviceId, currentDefault.DeviceId, ignoreCase: true);
        }
        finally
        {
            HardwareTestHelpers.RunOrSkip(
                () => controller.ApplyProfile(originalProfile),
                "default recording device restore");
        }
    }
}
