using Newtonsoft.Json;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class AudioFormatHardwareTests
{
    [SkippableFact]
    public void SetFormat_ShouldReapplyCurrentPlaybackFormat_WhenFormatIsReadable()
    {
        using WindowsAudioController controller = new();

        AudioProfile originalProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        originalProfile.EnsureDefaults();
        Skip.If(!originalProfile.Playback.IsFormatEnabled, "Skipping because playback format capture is not enabled.");
        Skip.If(!originalProfile.Playback.MultimediaDevice.IsEndpointEnabled, "Skipping because the playback multimedia device was not captured.");
        Skip.If(originalProfile.Playback.StreamFormat.SampleRate <= 0, "Skipping because the playback format is empty.");

        HardwareTestHelpers.RunOrSkip(
            () => controller.SetFormat(originalProfile.Playback.MultimediaDevice.DeviceId, originalProfile.Playback.StreamFormat),
            "playback format same-format reapply");

        AudioProfile currentProfile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture after playback format reapply");

        Assert.Equal(originalProfile.Playback.StreamFormat.SampleRate, currentProfile.Playback.StreamFormat.SampleRate);
        Assert.Equal(originalProfile.Playback.StreamFormat.BitsPerSample, currentProfile.Playback.StreamFormat.BitsPerSample);
        Assert.Equal(originalProfile.Playback.StreamFormat.Channels, currentProfile.Playback.StreamFormat.Channels);
        Assert.Equal(originalProfile.Playback.StreamFormat.ChannelMask, currentProfile.Playback.StreamFormat.ChannelMask);
        Assert.Equal(originalProfile.Playback.StreamFormat.SampleFormat, currentProfile.Playback.StreamFormat.SampleFormat);
    }

    [SkippableFact]
    public void AudioProfileJson_ShouldRoundTripSampleFormat()
    {
        var profile = new AudioProfile
        {
            Playback = new PlaybackAudioProfile
            {
                StreamFormat = new AudioFormatProfile
                {
                    SampleRate = 48000,
                    BitsPerSample = 32,
                    Channels = 2,
                    SampleFormat = AudioSampleFormat.IeeeFloat
                }
            }
        };

        string json = JsonConvert.SerializeObject(profile);
        AudioProfile? roundTripped = JsonConvert.DeserializeObject<AudioProfile>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(AudioSampleFormat.IeeeFloat, roundTripped.Playback.StreamFormat.SampleFormat);
    }
}
