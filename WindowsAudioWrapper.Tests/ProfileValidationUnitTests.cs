namespace WindowsAudioWrapper.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using WindowsAudioWrapper.Models;
using Xunit;

/// <summary>
/// Contains unit tests verifying the software business rules within the profile validation engine.
/// Runs entirely in memory without modifying live hardware states.
/// </summary>
public sealed class ProfileValidationUnitTests
{
    [Fact]
    public void ValidateProfile_ShouldPass_WhenProfileIsEmptyButValid()
    {
        using WindowsAudioController controller = new();
        AudioProfile profile = new();

        AudioProfileValidationResult result = controller.ValidateProfile(profile);

        Assert.True(result.Successful);
        Assert.Equal(AudioValidationSeverity.Valid, result.Severity);
        Assert.Empty(result.Messages);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    [InlineData(150)]
    public void ValidateProfile_ShouldReturnError_WhenPlaybackVolumeIsOutOfBounds(decimal invalidVolume)
    {
        using WindowsAudioController controller = new();
        AudioProfile profile = new();
        profile.Playback.IsPlaybackEnabled = true;
        profile.Playback.IsVolumeEnabled = true;
        profile.Playback.MultimediaDevice.IsEndpointEnabled = true;
        profile.Playback.MultimediaDevice.DeviceId = "{mock-playback-device}";
        profile.Playback.VolumePercent = invalidVolume;

        AudioProfileValidationResult result = controller.ValidateProfile(profile);

        Assert.False(result.Successful);
        Assert.Equal(AudioValidationSeverity.Error, result.Severity);
        Assert.Contains(result.Messages, m => m.Code == AudioMessageCode.InvalidVolume && m.Severity == AudioMessageSeverity.Error);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(105)]
    public void ValidateProfile_ShouldReturnError_WhenRecordingVolumeIsOutOfBounds(decimal invalidVolume)
    {
        using WindowsAudioController controller = new();
        AudioProfile profile = new();
        profile.Recording.IsRecordingEnabled = true;
        profile.Recording.IsVolumeEnabled = true;
        profile.Recording.MultimediaDevice.IsEndpointEnabled = true;
        profile.Recording.MultimediaDevice.DeviceId = "{mock-recording-device}";
        profile.Recording.VolumePercent = invalidVolume;

        AudioProfileValidationResult result = controller.ValidateProfile(profile);

        Assert.False(result.Successful);
        Assert.Equal(AudioValidationSeverity.Error, result.Severity);
        Assert.Contains(result.Messages, m => m.Code == AudioMessageCode.InvalidVolume && m.Severity == AudioMessageSeverity.Error);
    }

    [Fact]
    public void ValidateProfile_ShouldReturnError_WhenDeviceIsEnabledButReferenceIsMissing()
    {
        using WindowsAudioController controller = new();
        AudioProfile profile = new();
        profile.Playback.IsPlaybackEnabled = true;
        profile.Playback.IsVolumeEnabled = true;
        profile.Playback.MultimediaDevice.IsEndpointEnabled = false;

        AudioProfileValidationResult result = controller.ValidateProfile(profile);

        Assert.False(result.Successful);
        Assert.Equal(AudioValidationSeverity.Error, result.Severity);
        Assert.Contains(result.Messages, m => m.Code == AudioMessageCode.DeviceMissing && m.Severity == AudioMessageSeverity.Error);
    }

    [Fact]
    public void ValidateProfile_ShouldReturnError_WhenPlaybackDeviceHasRecordingFlowMismatched()
    {
        using WindowsAudioController controller = new();
        AudioProfile profile = new();
        profile.Playback.IsPlaybackEnabled = true;
        profile.Playback.MultimediaDevice.IsEndpointEnabled = true;
        profile.Playback.MultimediaDevice.DeviceId = "{mock-playback-device}";
        profile.Playback.MultimediaDevice.Flow = AudioFlow.Capture;

        AudioProfileValidationResult result = controller.ValidateProfile(profile);

        Assert.False(result.Successful);
        Assert.Equal(AudioValidationSeverity.Error, result.Severity);
        Assert.Contains(result.Messages, m => m.Code == AudioMessageCode.InvalidAudioFlow && m.Severity == AudioMessageSeverity.Error);
    }
}
