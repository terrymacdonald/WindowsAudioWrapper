using WindowsAudioWrapper.Models;
using WindowsAudioWrapper.Providers;
using Xunit;

namespace WindowsAudioWrapper.Tests;

public sealed class BestEffortApplyTests
{
    [Fact]
    public void ApplyProfile_ShouldApplyIndependentSettings_WhenPlaybackTargetIsMissing()
    {
        var deviceProvider = new FakeDeviceProvider();
        var defaultDeviceProvider = new FakeDefaultAudioDeviceProvider();
        var volumeProvider = new RecordingVolumeProvider();
        var formatProvider = new NoOpFormatProvider();
        var enhancementProvider = new NoOpAudioEnhancementProvider();
        var systemProvider = new RecordingSystemAudioProvider();

        using var controller = new WindowsAudioController(
            deviceProvider,
            defaultDeviceProvider,
            volumeProvider,
            formatProvider,
            enhancementProvider,
            systemProvider);

        AudioProfile profile = new()
        {
            Playback = new PlaybackAudioProfile
            {
                IsPlaybackEnabled = true,
                IsVolumeEnabled = true,
                MultimediaDevice = new AudioEndpointReference
                {
                    DeviceId = "missing-playback-device",
                    Flow = AudioFlow.Render,
                    IsEndpointEnabled = true
                },
                VolumePercent = 25
            },
            Recording = new RecordingAudioProfile
            {
                IsRecordingEnabled = true,
                IsVolumeEnabled = true,
                MultimediaDevice = new AudioEndpointReference
                {
                    DeviceId = FakeDeviceProvider.PresentRecordingDeviceId,
                    Flow = AudioFlow.Capture,
                    IsEndpointEnabled = true
                },
                VolumePercent = 65
            },
            System = new SystemAudioProfile
            {
                IsSystemAudioEnabled = true,
                IsMonoAudioEnabled = true,
                MonoAudio = true
            }
        };

        AudioProfileApplyResult result = controller.ApplyProfile(profile);

        Assert.False(result.Successful);
        Assert.Contains(result.Messages, message => message.Severity == AudioMessageSeverity.Error && message.Code == AudioMessageCode.DeviceUnavailable);
        Assert.DoesNotContain(volumeProvider.SetVolumeCalls, call => call.DeviceId == "missing-playback-device");
        Assert.Contains(volumeProvider.SetVolumeCalls, call => call.DeviceId == FakeDeviceProvider.PresentRecordingDeviceId && call.VolumePercent == 65);
        Assert.True(systemProvider.SetMonoAudioCalled);
        Assert.True(systemProvider.LastMonoAudioValue);
    }

    private sealed class FakeDeviceProvider : IAudioDeviceProvider
    {
        public const string PresentRecordingDeviceId = "present-recording-device";

        public IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states) => Array.Empty<AudioEndpointInfo>();

        public IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states) =>
            new[]
            {
                new AudioEndpointInfo
                {
                    DeviceId = PresentRecordingDeviceId,
                    Flow = AudioFlow.Capture,
                    State = AudioDeviceState.Active,
                    Capabilities = new AudioEndpointCapabilities
                    {
                        IsVolumeSupported = true,
                        IsMuteSupported = true
                    }
                }
            };

        public AudioEndpointInfo ResolveEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow)
        {
            if (endpoint.DeviceId == PresentRecordingDeviceId && expectedFlow == AudioFlow.Capture)
            {
                return new AudioEndpointInfo
                {
                    DeviceId = PresentRecordingDeviceId,
                    Flow = AudioFlow.Capture,
                    State = AudioDeviceState.Active,
                    Capabilities = new AudioEndpointCapabilities
                    {
                        IsVolumeSupported = true,
                        IsMuteSupported = true
                    }
                };
            }

            return new AudioEndpointInfo
            {
                DeviceId = endpoint.DeviceId,
                Flow = expectedFlow,
                State = AudioDeviceState.NotPresent
            };
        }
    }

    private sealed class FakeDefaultAudioDeviceProvider : IDefaultAudioDeviceProvider
    {
        public AudioEndpointInfo GetDefaultPlaybackDevice() => Missing(AudioFlow.Render);
        public AudioEndpointInfo GetDefaultConsolePlaybackDevice() => Missing(AudioFlow.Render);
        public AudioEndpointInfo GetDefaultRecordingDevice() => Missing(AudioFlow.Capture);
        public AudioEndpointInfo GetDefaultConsoleRecordingDevice() => Missing(AudioFlow.Capture);
        public AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice() => Missing(AudioFlow.Render);
        public AudioEndpointInfo GetDefaultCommunicationsRecordingDevice() => Missing(AudioFlow.Capture);
        public void SetDefaultPlaybackDevice(AudioEndpointReference endpoint) { }
        public void SetDefaultConsolePlaybackDevice(AudioEndpointReference endpoint) { }
        public void SetDefaultRecordingDevice(AudioEndpointReference endpoint) { }
        public void SetDefaultConsoleRecordingDevice(AudioEndpointReference endpoint) { }
        public void SetDefaultCommunicationsPlaybackDevice(AudioEndpointReference endpoint) { }
        public void SetDefaultCommunicationsRecordingDevice(AudioEndpointReference endpoint) { }

        private static AudioEndpointInfo Missing(AudioFlow flow) => new()
        {
            Flow = flow,
            State = AudioDeviceState.NotPresent
        };
    }

    private sealed class RecordingVolumeProvider : IAudioVolumeProvider
    {
        public List<(string DeviceId, decimal VolumePercent)> SetVolumeCalls { get; } = new();

        public decimal GetVolumePercent(AudioEndpointReference endpoint) => 0;

        public void SetVolumePercent(AudioEndpointReference endpoint, decimal volumePercent)
        {
            SetVolumeCalls.Add((endpoint.DeviceId, volumePercent));
        }

        public bool GetMute(AudioEndpointReference endpoint) => false;

        public void SetMute(AudioEndpointReference endpoint, bool muted) { }
    }

    private sealed class NoOpFormatProvider : IAudioFormatProvider
    {
        public AudioFormatProfile GetFormat(string deviceId) => new();

        public void SetFormat(AudioEndpointReference endpoint, AudioFormatProfile format) { }
    }

    private sealed class NoOpAudioEnhancementProvider : IAudioEnhancementProvider
    {
        public AudioEnhancementProfile GetAudioEnhancements(string deviceId) => new();

        public void SetAudioEnhancements(AudioEndpointReference endpoint, AudioEnhancementProfile audioEnhancements) { }
    }

    private sealed class RecordingSystemAudioProvider : ISystemAudioProvider
    {
        public bool IsMonoAudioReadSupported => true;
        public bool IsMonoAudioSetSupported => true;
        public bool SetMonoAudioCalled { get; private set; }
        public bool LastMonoAudioValue { get; private set; }

        public bool GetMonoAudio() => false;

        public void SetMonoAudio(bool enabled)
        {
            SetMonoAudioCalled = true;
            LastMonoAudioValue = enabled;
        }
    }
}
