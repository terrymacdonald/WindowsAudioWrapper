using System.Text.Json;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

internal static class HardwareTestHelpers
{
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

    public static void SkipIfNoDevices(IReadOnlyList<AudioEndpointInfo> devices, string deviceType)
    {
        Skip.If(devices.Count == 0, $"Skipping because this machine has no {deviceType} devices returned by WindowsAudioWrapper.");
    }

    public static void SkipIfNoActiveDevice(AudioEndpointInfo endpoint, string deviceType)
    {
        Skip.If(!endpoint.IsAvailable, $"Skipping because this machine does not have an active default {deviceType} device.");
    }

    public static void SkipIfPlaybackVolumeUnsupported(AudioProfile profile)
    {
        profile.EnsureDefaults();
        Skip.If(!profile.Playback.IsPlaybackEnabled, "Skipping because playback capture is not enabled in the current profile.");
        Skip.If(!profile.Playback.IsVolumeEnabled, "Skipping because playback volume is not readable or not supported on this machine.");
        Skip.If(!profile.Playback.Device.IsEndpointEnabled, "Skipping because the current playback device could not be captured.");
    }

    public static void SkipIfPlaybackMuteUnsupported(AudioProfile profile)
    {
        profile.EnsureDefaults();
        Skip.If(!profile.Playback.IsPlaybackEnabled, "Skipping because playback capture is not enabled in the current profile.");
        Skip.If(!profile.Playback.IsMuteEnabled, "Skipping because playback mute is not readable or not supported on this machine.");
        Skip.If(!profile.Playback.Device.IsEndpointEnabled, "Skipping because the current playback device could not be captured.");
    }

    public static void SkipIfRecordingVolumeUnsupported(AudioProfile profile)
    {
        profile.EnsureDefaults();
        Skip.If(!profile.Recording.IsRecordingEnabled, "Skipping because recording capture is not enabled in the current profile.");
        Skip.If(!profile.Recording.IsVolumeEnabled, "Skipping because recording volume is not readable or not supported on this machine.");
        Skip.If(!profile.Recording.Device.IsEndpointEnabled, "Skipping because the current recording device could not be captured.");
    }

    public static AudioProfile CloneProfile(AudioProfile profile)
    {
        profile.EnsureDefaults();
        string json = JsonSerializer.Serialize(profile);
        AudioProfile? clone = JsonSerializer.Deserialize<AudioProfile>(json);
        Assert.NotNull(clone);
        clone.EnsureDefaults();
        return clone;
    }

    public static AudioProfile BuildPlaybackVolumeProfile(AudioProfile source, decimal volumePercent)
    {
        source.EnsureDefaults();

        return new AudioProfile
        {
            Playback =
            {
                IsPlaybackEnabled = true,
                Device = CloneEndpoint(source.Playback.Device),
                IsVolumeEnabled = true,
                VolumePercent = ClampVolume(volumePercent)
            }
        };
    }

    public static AudioProfile BuildPlaybackMuteProfile(AudioProfile source, bool isMuted)
    {
        source.EnsureDefaults();

        return new AudioProfile
        {
            Playback =
            {
                IsPlaybackEnabled = true,
                Device = CloneEndpoint(source.Playback.Device),
                IsMuteEnabled = true,
                IsMuted = isMuted
            }
        };
    }

    public static AudioProfile BuildRecordingVolumeProfile(AudioProfile source, decimal volumePercent)
    {
        source.EnsureDefaults();

        return new AudioProfile
        {
            Recording =
            {
                IsRecordingEnabled = true,
                Device = CloneEndpoint(source.Recording.Device),
                IsVolumeEnabled = true,
                VolumePercent = ClampVolume(volumePercent)
            }
        };
    }

    public static AudioProfile BuildDefaultPlaybackDeviceProfile(AudioEndpointInfo endpoint)
    {
        return new AudioProfile
        {
            Playback =
            {
                IsPlaybackEnabled = true,
                Device = AudioEndpointReference.FromEndpointInfo(endpoint),
                IsDefaultPlaybackDeviceEnabled = true
            }
        };
    }

    public static AudioProfile BuildDefaultRecordingDeviceProfile(AudioEndpointInfo endpoint)
    {
        return new AudioProfile
        {
            Recording =
            {
                IsRecordingEnabled = true,
                Device = AudioEndpointReference.FromEndpointInfo(endpoint),
                IsDefaultRecordingDeviceEnabled = true
            }
        };
    }

    public static decimal AlternativeVolume(decimal currentVolumePercent)
    {
        decimal rounded = Math.Round(currentVolumePercent, 0, MidpointRounding.AwayFromZero);
        return rounded >= 50 ? 35 : 65;
    }

    public static void AssertApplySucceeded(AudioProfileApplyResult result)
    {
        string messages = string.Join(Environment.NewLine, result.Messages.Select(message => $"{message.Severity}: {message.Code}: {message.Message}"));
        Assert.True(result.Successful, messages);
    }

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
            // FIX: Deep clone hardware descriptors so resolution engines can find anchors safely
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
        if (volumePercent < 0)
        {
            return 0;
        }

        if (volumePercent > 100)
        {
            return 100;
        }

        return volumePercent;
    }
}