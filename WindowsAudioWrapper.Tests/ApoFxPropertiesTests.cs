using Newtonsoft.Json;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

public sealed class ApoFxPropertiesTests
{
    [Fact]
    public void AudioEndpointReferenceJson_ShouldRoundTripApoFxProperties()
    {
        var reference = new AudioEndpointReference
        {
            DeviceId = "endpoint-id",
            FriendlyName = "Endpoint",
            ApoFxProperties = new Dictionary<string, string>
            {
                ["{11111111-1111-1111-1111-111111111111},1"] = "int:25",
                ["{22222222-2222-2222-2222-222222222222},2"] = "hex:01020304"
            }
        };

        string json = JsonConvert.SerializeObject(reference);
        AudioEndpointReference? roundTripped = JsonConvert.DeserializeObject<AudioEndpointReference>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(reference.ApoFxProperties, roundTripped.ApoFxProperties);
    }

    [Fact]
    public void PlaybackProfileEnsureDefaults_ShouldInitializeRoleEndpointApoFxProperties()
    {
        var profile = new PlaybackAudioProfile
        {
            MultimediaDevice = new AudioEndpointReference { DeviceId = "multimedia" },
            ConsoleDevice = new AudioEndpointReference { DeviceId = "console" },
            CommunicationsDevice = new AudioEndpointReference { DeviceId = "communications" }
        };

        profile.EnsureDefaults();

        Assert.NotNull(profile.MultimediaDevice.ApoFxProperties);
        Assert.NotNull(profile.ConsoleDevice.ApoFxProperties);
        Assert.NotNull(profile.CommunicationsDevice.ApoFxProperties);
    }
}
