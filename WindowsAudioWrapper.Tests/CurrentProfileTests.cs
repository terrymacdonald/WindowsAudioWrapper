using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class CurrentProfileTests
{
    [SkippableFact]
    public void GetCurrentProfile_ShouldCaptureAllReadableSettingsIntoAudioProfile()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        Assert.NotNull(profile);
        Assert.NotNull(profile.Playback);
        Assert.NotNull(profile.Recording);
        Assert.NotNull(profile.System);

        Skip.If(!profile.IsAnyAudioSettingEnabled, "Skipping because no readable Windows audio settings were captured on this machine.");

        if (profile.Playback.IsPlaybackEnabled)
        {
            Assert.True(
                profile.Playback.IsDefaultPlaybackDeviceEnabled ||
                profile.Playback.IsDefaultCommunicationsPlaybackDeviceEnabled ||
                profile.Playback.IsVolumeEnabled ||
                profile.Playback.IsMuteEnabled ||
                profile.Playback.IsFormatEnabled ||
                profile.Playback.IsSpatialSoundEnabled ||
                profile.Playback.IsAudioEnhancementsEnabled,
                "Playback was enabled but no playback settings were captured.");
        }

        if (profile.Recording.IsRecordingEnabled)
        {
            Assert.True(
                profile.Recording.IsDefaultRecordingDeviceEnabled ||
                profile.Recording.IsDefaultCommunicationsRecordingDeviceEnabled ||
                profile.Recording.IsVolumeEnabled ||
                profile.Recording.IsMuteEnabled ||
                profile.Recording.IsFormatEnabled ||
                profile.Recording.IsAudioEnhancementsEnabled ||
                profile.Recording.IsVoiceProcessingEnabled,
                "Recording was enabled but no recording settings were captured.");
        }
    }

    [SkippableFact]
    public void GetCurrentProfile_ShouldReturnProfileThatValidatesSuccessfully()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        AudioProfileValidationResult validation = HardwareTestHelpers.RunOrSkip(
            () => controller.ValidateProfile(profile),
            "audio profile validation");

        string messages = string.Join(Environment.NewLine, validation.Messages.Select(message => $"{message.Severity}: {message.Code}: {message.Message}"));
        Assert.True(validation.Successful, messages);
    }

    [SkippableFact]
    public void ApplyProfile_ShouldReapplyCurrentProfileSuccessfully()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        AudioProfileApplyResult result = HardwareTestHelpers.RunOrSkip(
            () => controller.ApplyProfile(originalProfile),
            "current audio profile apply");

        HardwareTestHelpers.AssertApplySucceeded(result);
    }
}
