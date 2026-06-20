using System;
using System.Collections.Generic;
using System.Linq;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

/// <summary>
/// Validates the direct per-feature methods on the controller and the macro flag hydration utilities.
/// </summary>
[Collection(AudioHardwareCollection.Name)]
public sealed class DirectApiHardwareTests
{
    /// <summary>
    /// Verifies that calling EnableAllFeatures successfully forces every hidden control flag to true.
    /// </summary>
    [Fact]
    public void EnableAllFeatures_ShouldFlipAllHiddenTelemetryFlagsToTrue()
    {
        AudioProfile profile = new();
        profile.Playback.StreamFormat.SampleRate = 48000;
        profile.Recording.StreamFormat.SampleRate = 48000;

        // Execute the macro hydration target hook
        profile.EnableAllFeatures();

        // Assert Playback Flags
        Assert.True(profile.Playback.IsPlaybackEnabled);
        Assert.True(profile.Playback.IsDefaultPlaybackDeviceEnabled);
        Assert.True(profile.Playback.IsDefaultCommunicationsPlaybackDeviceEnabled);
        Assert.True(profile.Playback.IsVolumeEnabled);
        Assert.True(profile.Playback.IsMuteEnabled);
        Assert.True(profile.Playback.IsFormatEnabled);
        Assert.True(profile.Playback.IsAudioEnhancementsEnabled);

        // Assert Recording Flags
        Assert.True(profile.Recording.IsRecordingEnabled);
        Assert.True(profile.Recording.IsDefaultRecordingDeviceEnabled);
        Assert.True(profile.Recording.IsDefaultCommunicationsRecordingDeviceEnabled);
        Assert.True(profile.Recording.IsVolumeEnabled);
        Assert.True(profile.Recording.IsMuteEnabled);
        Assert.True(profile.Recording.IsFormatEnabled);
        Assert.True(profile.Recording.IsAudioEnhancementsEnabled);

        // Assert System Flags
        Assert.True(profile.System.IsSystemAudioEnabled);
        Assert.True(profile.System.IsMonoAudioEnabled);
    }

    /// <summary>
    /// Tests the direct SetVolumePercent method on the active default playback device and restores it.
    /// </summary>
    [SkippableFact]
    public void SetVolumePercent_ShouldDirectlyAlterHardwareVolume_WithoutProfileObject()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo defaultPlayback = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultPlaybackDevice,
            "default playback device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(defaultPlayback, "playback");
        Skip.If(!defaultPlayback.Capabilities.IsVolumeSupported, "Skipping because volume adjustments are unsupported on this endpoint.");

        decimal originalVolume = defaultPlayback.VolumePercent;
        decimal targetVolume = HardwareTestHelpers.AlternativeVolume(originalVolume);

        try
        {
            // Execute granular per-feature method directly
            HardwareTestHelpers.RunOrSkip(
                () => controller.SetVolumePercent(defaultPlayback.DeviceId, targetVolume),
                "direct volume modification");

            AudioEndpointInfo updatedPlayback = controller.GetDefaultPlaybackDevice();
            HardwareTestHelpers.AssertVolumeClose(updatedPlayback.VolumePercent, targetVolume);
        }
        finally
        {
            // Direct clean state cleanup restoration line
            controller.SetVolumePercent(defaultPlayback.DeviceId, originalVolume);
        }
    }

    /// <summary>
    /// Tests the direct SetMute method on the active default playback device and restores it.
    /// </summary>
    [SkippableFact]
    public void SetMute_ShouldDirectlyToggleHardwareMute_WithoutProfileObject()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo defaultPlayback = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultPlaybackDevice,
            "default playback device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(defaultPlayback, "playback");
        Skip.If(!defaultPlayback.Capabilities.IsMuteSupported, "Skipping because mute adjustments are unsupported on this endpoint.");

        bool originalMuteState = defaultPlayback.IsMuted;
        bool targetMuteState = !originalMuteState;

        try
        {
            // Execute granular per-feature method directly
            HardwareTestHelpers.RunOrSkip(
                () => controller.SetMute(defaultPlayback.DeviceId, targetMuteState),
                "direct mute modification");

            AudioEndpointInfo updatedPlayback = controller.GetDefaultPlaybackDevice();
            Assert.Equal(targetMuteState, updatedPlayback.IsMuted);
        }
        finally
        {
            controller.SetMute(defaultPlayback.DeviceId, originalMuteState);
        }
    }

    /// <summary>
    /// Verifies the direct SetDefaultPlaybackDevice routing switch method when alternative hardware targets exist.
    /// </summary>
    [SkippableFact]
    public void SetDefaultPlaybackDevice_ShouldDirectlySwapSystemRouting_WithoutProfileObject()
    {
        using WindowsAudioController controller = new();

        IReadOnlyList<AudioEndpointInfo> activePlaybackDevices = HardwareTestHelpers.RunOrSkip(
            () => controller.GetPlaybackDevices(AudioDeviceState.Active),
            "active playback device enumeration");

        HardwareTestHelpers.SkipIfNoDevices(activePlaybackDevices, "active playback");
        Skip.If(activePlaybackDevices.Count < 2, "Skipping because testing routing transitions requires multiple active devices.");

        AudioEndpointInfo originalDefault = controller.GetDefaultPlaybackDevice();
        AudioEndpointInfo alternateTarget = activePlaybackDevices.First(d => 
            !d.DeviceId.Equals(originalDefault.DeviceId, StringComparison.OrdinalIgnoreCase));

        try
        {
            // Execute direct routing assignment method
            HardwareTestHelpers.RunOrSkip(
                () => controller.SetDefaultPlaybackDevice(alternateTarget.DeviceId),
                "direct default device routing swap");

            AudioEndpointInfo currentDefault = controller.GetDefaultPlaybackDevice();
            Assert.Equal(alternateTarget.DeviceId, currentDefault.DeviceId, ignoreCase: true);
        }
        finally
        {
            controller.SetDefaultPlaybackDevice(originalDefault.DeviceId);
        }
    }
}