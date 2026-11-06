namespace WindowsAudioWrapper.Tests;

using Newtonsoft.Json;
using WindowsAudioWrapper.Models;

public sealed class ResolvedEndpointApplyTests
{
    [Theory]
    [InlineData(AudioFlow.Render)]
    [InlineData(AudioFlow.Capture)]
    public void AllSettingsUseResolvedIdentityAndPreserveSavedReferences(AudioFlow flow)
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("actual", flow));
        using WindowsAudioController controller = providers.Controller();
        AudioEndpointReference saved = new() { DeviceId = "saved", ContainerId = "container", Flow = flow, ApoFxProperties = new() { ["effect"] = "value" } };
        AudioProfile profile = new();
        if (flow == AudioFlow.Render)
            profile.Playback = new() { MultimediaDevice = saved, ConsoleDevice = saved, CommunicationsDevice = saved, VolumePercent = 50,
                VolumeLeft = 30, VolumeRight = 40, StreamFormat = new() { SampleRate = 48000 }, SpeakerConfiguration = new() { ChannelMask = 3 } };
        else
            profile.Recording = new() { MultimediaDevice = saved, ConsoleDevice = saved, CommunicationsDevice = saved, VolumePercent = 50,
                VolumeLeft = 30, VolumeRight = 40, StreamFormat = new() { SampleRate = 48000, ChannelMask = 3 } };
        profile.EndpointVisibilities.Add(new() { DeviceId = "saved", ContainerId = "container", Flow = flow });
        string original = JsonConvert.SerializeObject(profile);
        AudioProfileApplyResult result = controller.ApplyProfile(profile);
        Assert.True(result.Successful, string.Join("; ", result.Messages.Select(message => message.Message)));
        Assert.Equal("actual", result.ResolvedDeviceIds["saved"]);
        Assert.Single(result.ResolvedDeviceIds);
        Assert.NotEmpty(providers.Writes);
        Assert.All(providers.Writes, write => Assert.Equal("actual", write.Id));
        foreach (string setting in new[] { "visibility", "volume", "mute", "format", "channels", "properties" })
            Assert.Contains(providers.Writes, write => write.Setting == setting);
        Assert.Equal(original, JsonConvert.SerializeObject(profile));
    }

    [Fact]
    public void AmbiguityFailsValidationAndApplyWithoutWrites()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("one")); providers.Devices.Add(EndpointTestProviders.Device("two"));
        using WindowsAudioController controller = providers.Controller();
        AudioProfile profile = new() { Playback = new() { MultimediaDevice = new() { DeviceId = "old", ContainerId = "container" }, VolumePercent = 40 } };
        Assert.Contains(controller.ValidateProfile(profile).Messages, message => message.Code == AudioMessageCode.AmbiguousEndpoint);
        Assert.Contains(controller.ApplyProfile(profile).Messages, message => message.Code == AudioMessageCode.AmbiguousEndpoint);
        Assert.Empty(providers.Writes);
    }

    [Fact]
    public void IndependentRolesContinueAfterAVolumeFailure()
    {
        EndpointTestProviders providers = new() { ThrowOnVolume = true }; providers.Devices.Add(EndpointTestProviders.Device("actual"));
        using WindowsAudioController controller = providers.Controller();
        AudioProfile profile = new() { Playback = new() { MultimediaDevice = new() { DeviceId = "actual" }, CommunicationsDevice = new() { DeviceId = "actual" } } };
        AudioProfileApplyResult result = controller.ApplyProfile(profile);
        Assert.False(result.Successful);
        Assert.Contains(providers.Writes, write => write.Setting == "communications-playback");
        Assert.Contains(providers.Writes, write => write.Setting == "mute");
    }
}
