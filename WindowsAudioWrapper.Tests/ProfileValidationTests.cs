using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class ProfileValidationTests
{
    [SkippableFact]
    public void ValidateProfile_ShouldReturnError_WhenPlaybackVolumeEnabledWithoutDevice()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = new()
        {
            Playback =
            {
                IsPlaybackEnabled = true,
                IsVolumeEnabled = true,
                VolumePercent = 50
            }
        };

        AudioProfileValidationResult validation = HardwareTestHelpers.RunOrSkip(
            () => controller.ValidateProfile(profile),
            "audio profile validation");

        Assert.False(validation.Successful);
        Assert.Contains(validation.Messages, message => message.Code == AudioMessageCode.DeviceMissing);
    }

    [SkippableFact]
    public void ValidateProfile_ShouldReturnError_WhenPlaybackVolumeIsOutOfRange()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = new()
        {
            Playback =
            {
                IsPlaybackEnabled = true,
                Device =
                {
                    DeviceId = "test-device",
                    Flow = AudioFlow.Render
                },
                IsVolumeEnabled = true,
                VolumePercent = 101
            }
        };

        AudioProfileValidationResult validation = HardwareTestHelpers.RunOrSkip(
            () => controller.ValidateProfile(profile),
            "audio profile validation");

        Assert.False(validation.Successful);
        Assert.Contains(validation.Messages, message => message.Code == AudioMessageCode.InvalidVolume);
    }

    [SkippableFact]
    public void ValidateProfile_ShouldReturnError_WhenPlaybackDeviceHasCaptureFlow()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = new()
        {
            Playback =
            {
                IsPlaybackEnabled = true,
                IsDefaultPlaybackDeviceEnabled = true,
                Device =
                {
                    DeviceId = "test-device",
                    Flow = AudioFlow.Capture
                }
            }
        };

        AudioProfileValidationResult validation = HardwareTestHelpers.RunOrSkip(
            () => controller.ValidateProfile(profile),
            "audio profile validation");

        Assert.False(validation.Successful);
        Assert.Contains(validation.Messages, message => message.Code == AudioMessageCode.InvalidAudioFlow);
    }

    [SkippableFact]
    public void ValidateProfile_ShouldReturnWarning_WhenSystemAudioEnabledWithoutSettings()
    {
        using WindowsAudioController controller = new();

        AudioProfile profile = new()
        {
            System =
            {
                IsSystemAudioEnabled = true,
                IsMonoAudioEnabled = false
            }
        };

        AudioProfileValidationResult validation = HardwareTestHelpers.RunOrSkip(
            () => controller.ValidateProfile(profile),
            "audio profile validation");

        Assert.True(validation.Successful);
        Assert.Equal(AudioValidationSeverity.Warning, validation.Severity);
        Assert.Contains(validation.Messages, message => message.Code == AudioMessageCode.NoEnabledSettings);
    }
}
