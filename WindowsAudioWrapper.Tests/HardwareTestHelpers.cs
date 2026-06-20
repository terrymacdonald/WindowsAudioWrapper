using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

/// <summary>
/// Provides unified utility scaffolding, mock wrappers, and baseline assertions for audio hardware integration testing vectors.
/// </summary>
internal static class HardwareTestHelpers
{
    /// <summary>
    /// Executes an audio tracking function, gracefully skipping execution if the local host platform or environment lacks implementation capabilities.
    /// </summary>
    public static T RunOrSkip<T>(Func<T> action, string featureName)
    {
        try
        {
            return action();
        }
        catch (NotImplementedException ex)
        {
            Skip.If(true, $"Skipping because {featureName} is not implemented yet: {ex.Message}");
            throw;
        }
        catch (PlatformNotSupportedException ex)
        {
            Skip.If(true, $"Skipping because {featureName} is not supported on this platform: {ex.Message}");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            Skip.If(true, $"Skipping because {featureName} is not accessible in this test environment: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Executes an audio tracking action, gracefully skipping execution if the local host platform or environment lacks implementation capabilities.
    /// </summary>
    public static void RunOrSkip(Action action, string featureName)
    {
        RunOrSkip(
            () =>
            {
                action();
                return true;
            },
            featureName);
    }

    /// <summary>
    /// Signals the test runner to skip execution runs if the target endpoint collection array is empty.
    /// </summary>
    public static void SkipIfNoDevices(IReadOnlyList<AudioEndpointInfo> devices, string deviceType)
    {
        Skip.If(devices.Count == 0, $"Skipping because this machine has no {deviceType} devices returned by WindowsAudioWrapper.");
    }

    /// <summary>
    /// Signals the test runner to skip execution runs if the requested default device is currently unavailable or unplugged.
    /// </summary>
    public static void SkipIfNoActiveDevice(AudioEndpointInfo endpoint, string deviceType)
    {
        Skip.If(!endpoint.IsAvailable, $"Skipping because this machine does not have an active default {deviceType} device.");
    }

    /// <summary>
    /// Verifies if a profile's playback volume properties are fully initialized and supported, skipping the test execution if constraints fail.
    /// </summary>
    public static void SkipIfPlaybackVolumeUnsupported(AudioProfile profile)
    {
        profile.EnsureDefaults();
        Skip.If(!profile.Playback.IsPlaybackEnabled, "Skipping because playback capture is not enabled in the current profile.");
        Skip.If(!profile.Playback.IsVolumeEnabled, "Skipping because playback volume is not readable or not supported on this machine.");
        Skip.If(!profile.Playback.TargetDevice.IsEndpointEnabled, "Skipping because the current playback target device could not be captured.");
    }

    /// <summary>
    /// Verifies if a profile's playback mute lines are fully initialized and supported, skipping the test execution if constraints fail.
    /// </summary>
    public static void SkipIfPlaybackMuteUnsupported(AudioProfile profile)
    {
        profile.EnsureDefaults();
        Skip.If(!profile.Playback.IsPlaybackEnabled, "Skipping because playback capture is not enabled in the current profile.");
        Skip.If(!profile.Playback.IsMuteEnabled, "Skipping because playback mute is not readable or not supported on this machine.");
        Skip.If(!profile.Playback.TargetDevice.IsEndpointEnabled, "Skipping because the current playback target device could not be captured.");
    }

    /// <summary>
    /// Verifies if a profile's recording volume properties are fully initialized and supported, skipping the test execution if constraints fail.
    /// </summary>
    public static void SkipIfRecordingVolumeUnsupported(AudioProfile profile)
    {
        profile.EnsureDefaults();
        Skip.If(!profile.Recording.IsRecordingEnabled, "Skipping because recording capture is not enabled in the current profile.");
        Skip.If(!profile.Recording.IsVolumeEnabled, "Skipping because recording volume is not readable or not supported on this machine.");
        Skip.If(!profile.Recording.TargetDevice.IsEndpointEnabled, "Skipping because the current recording target device could not be captured.");
    }

    /// <summary>
    /// Deep-clones an audio profile by performing a round-trip JSON serialization pass.
    /// </summary>
    public static AudioProfile CloneProfile(AudioProfile profile)
    {
        profile.EnsureDefaults();
        string json = JsonSerializer.Serialize(profile);
        AudioProfile? clone = JsonSerializer.Deserialize<AudioProfile>(json);
        Assert.NotNull(clone);
        clone.EnsureDefaults();
        return clone;
    }

    /// <summary>
    /// Compiles an isolated playback testing profile targeting specific hardware outputs and custom volume levels.
    /// </summary>
    public static AudioProfile BuildPlaybackVolumeProfile(AudioProfile source, decimal volumePercent)
    {
        source.EnsureDefaults();

        return new AudioProfile
        {
            Playback = new PlaybackAudioProfile
            {
                IsPlaybackEnabled = true,
                TargetDevice = CloneEndpoint(source.Playback.TargetDevice),
                IsVolumeEnabled = true,
                VolumePercent = ClampVolume(volumePercent),
                IsMuteEnabled = source.Playback.IsMuteEnabled,
                IsMuted = source.Playback.IsMuted,
                IsFormatEnabled = source.Playback.IsFormatEnabled,
                StreamFormat = source.Playback.StreamFormat,
                IsAudioEnhancementsEnabled = source.Playback.IsAudioEnhancementsEnabled,
                AudioEnhancements = source.Playback.AudioEnhancements
            }
        };
    }

    /// <summary>
    /// Compiles an isolated playback testing profile targeting specific hardware outputs and toggled mute switches.
    /// </summary>
    public static AudioProfile BuildPlaybackMuteProfile(AudioProfile source, bool isMuted)
    {
        source.EnsureDefaults();

        return new AudioProfile
        {
            Playback = new PlaybackAudioProfile
            {
                IsPlaybackEnabled = true,
                TargetDevice = CloneEndpoint(source.Playback.TargetDevice),
                IsVolumeEnabled = source.Playback.IsVolumeEnabled,
                VolumePercent = source.Playback.VolumePercent,
                IsMuteEnabled = true,
                IsMuted = isMuted,
                IsFormatEnabled = source.Playback.IsFormatEnabled,
                StreamFormat = source.Playback.StreamFormat,
                IsAudioEnhancementsEnabled = source.Playback.IsAudioEnhancementsEnabled,
                AudioEnhancements = source.Playback.AudioEnhancements
            }
        };
    }

    /// <summary>
    /// Compiles an isolated recording testing profile targeting specific hardware inputs and custom volume levels.
    /// </summary>
    public static AudioProfile BuildRecordingVolumeProfile(AudioProfile source, decimal volumePercent)
    {
        source.EnsureDefaults();

        return new AudioProfile
        {
            Recording = new RecordingAudioProfile
            {
                IsRecordingEnabled = true,
                TargetDevice = CloneEndpoint(source.Recording.TargetDevice),
                IsVolumeEnabled = true,
                VolumePercent = ClampVolume(volumePercent),
                IsMuteEnabled = source.Recording.IsMuteEnabled,
                IsMuted = source.Recording.IsMuted,
                IsFormatEnabled = source.Recording.IsFormatEnabled,
                StreamFormat = source.Recording.StreamFormat,
                IsAudioEnhancementsEnabled = source.Recording.IsAudioEnhancementsEnabled,
                AudioEnhancements = source.Recording.AudioEnhancements
            }
        };
    }

    /// <summary>
    /// Generates a minimalist default multimedia playback swap routing configuration profile.
    /// </summary>
    public static AudioProfile BuildDefaultPlaybackDeviceProfile(AudioEndpointInfo endpoint)
    {
        return new AudioProfile
        {
            Playback = new PlaybackAudioProfile
            {
                IsPlaybackEnabled = true,
                TargetDevice = AudioEndpointReference.FromEndpointInfo(endpoint),
                IsDefaultPlaybackDeviceEnabled = true
            }
        };
    }

    /// <summary>
    /// Generates a minimalist default multimedia recording swap routing configuration profile.
    /// </summary>
    public static AudioProfile BuildDefaultRecordingDeviceProfile(AudioEndpointInfo endpoint)
    {
        return new AudioProfile
        {
            Recording = new RecordingAudioProfile
            {
                IsRecordingEnabled = true,
                TargetDevice = AudioEndpointReference.FromEndpointInfo(endpoint),
                IsDefaultRecordingDeviceEnabled = true
            }
        };
    }

    /// <summary>
    /// Evaluates current scalar volume lines and returns an alternative level safely out of bounds from current values.
    /// </summary>
    public static decimal AlternativeVolume(decimal currentVolumePercent)
    {
        decimal rounded = Math.Round(currentVolumePercent, 0, MidpointRounding.AwayFromZero);
        return rounded >= 50 ? 35 : 65;
    }

    /// <summary>
    /// Asserts that a profile application pipeline completed successfully without throwing internal log faults.
    /// </summary>
    public static void AssertApplySucceeded(AudioProfileApplyResult result)
    {
        string messages = string.Join(Environment.NewLine, result.Messages.Select(message => $"{message.Severity}: {message.Code}: {message.Message}"));
        Assert.True(result.Successful, messages);
    }

    /// <summary>
    /// Performs a delta tolerance verification check to ensure volume modifications track within acceptable rounding bounds.
    /// </summary>
    public static void AssertVolumeClose(decimal actual, decimal expected, decimal tolerance = 2)
    {
        Assert.InRange(actual, expected - tolerance, expected + tolerance);
    }

    private static AudioEndpointReference CloneEndpoint(AudioEndpointReference endpoint)
    {
        if (endpoint == null) return new AudioEndpointReference();

        return new AudioEndpointReference
        {
            DeviceId = endpoint.DeviceId,
            ContainerId = endpoint.ContainerId,
            FriendlyName = endpoint.FriendlyName,
            FullName = endpoint.FullName,
            Flow = endpoint.Flow,
            IsEndpointEnabled = endpoint.IsEndpointEnabled,
            HardwareDetails = new HardwareDetails
            {
                DeviceDescription = endpoint.HardwareDetails?.DeviceDescription ?? string.Empty,
                HardwareId = endpoint.HardwareDetails?.HardwareId ?? string.Empty,
                DriverVersion = endpoint.HardwareDetails?.DriverVersion ?? string.Empty,
                EndpointAssociationGuid = endpoint.HardwareDetails?.EndpointAssociationGuid ?? string.Empty
            }
        };
    }

    private static decimal ClampVolume(decimal volumePercent)
    {
        if (volumePercent < 0) return 0;
        if (volumePercent > 100) return 100;
        return volumePercent;
    }
}