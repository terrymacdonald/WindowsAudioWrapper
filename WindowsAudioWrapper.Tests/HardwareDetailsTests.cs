using Newtonsoft.Json;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class HardwareDetailsTests
{
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointFormFactor = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 0);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointPhysicalSpeakers = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 3);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointGuid = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 4);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointFullRangeSpeakers = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 6);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointSupportsEventDrivenMode = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 7);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointJackSubType = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 8);
    private static readonly PROPERTYKEY SdkPkeyAudioEngineDeviceFormat = new(new Guid("F19F064D-082C-4E27-BC73-6882A1BB8E4C"), 0);

    [Fact]
    public void AudioEndpointReferenceFromEndpointInfo_ShouldPreserveAllHardwareDetails()
    {
        AudioEndpointInfo endpoint = new()
        {
            DeviceId = "{test-device}",
            State = AudioDeviceState.Active,
            HardwareDetails = new HardwareDetails
            {
                DeviceDescription = "Test device",
                FormFactorCode = 3,
                PhysicalSpeakersMask = 3,
                FullRangeSpeakersMask = 1,
                EndpointGuid = Guid.NewGuid().ToString("D"),
                DeviceFormatSummary = "FormatTag=65534;Channels=2;SampleRate=48000;BitsPerSample=24;BlockAlign=6;AvgBytesPerSec=288000",
                SupportsEventDrivenMode = true,
                JackSubType = Guid.NewGuid().ToString("D")
            }
        };

        AudioEndpointReference reference = AudioEndpointReference.FromEndpointInfo(endpoint);

        Assert.Equal(endpoint.HardwareDetails.DeviceDescription, reference.HardwareDetails.DeviceDescription);
        Assert.Equal(endpoint.HardwareDetails.FormFactorCode, reference.HardwareDetails.FormFactorCode);
        Assert.Equal(endpoint.HardwareDetails.PhysicalSpeakersMask, reference.HardwareDetails.PhysicalSpeakersMask);
        Assert.Equal(endpoint.HardwareDetails.FullRangeSpeakersMask, reference.HardwareDetails.FullRangeSpeakersMask);
        Assert.Equal(endpoint.HardwareDetails.EndpointGuid, reference.HardwareDetails.EndpointGuid);
        Assert.Equal(endpoint.HardwareDetails.DeviceFormatSummary, reference.HardwareDetails.DeviceFormatSummary);
        Assert.Equal(endpoint.HardwareDetails.SupportsEventDrivenMode, reference.HardwareDetails.SupportsEventDrivenMode);
        Assert.Equal(endpoint.HardwareDetails.JackSubType, reference.HardwareDetails.JackSubType);
    }

    [Fact]
    public void HardwareDetailsJson_ShouldAlwaysContainSpeakerMaskFields()
    {
        HardwareDetails details = new();

        string json = JsonConvert.SerializeObject(details);

        Assert.Contains("\"PhysicalSpeakersMask\":0", json);
        Assert.Contains("\"FullRangeSpeakersMask\":0", json);
        Assert.DoesNotContain("DeviceInstanceId", json);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureFormFactorFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        uint expected = ReadUInt32OrSkip(store, SdkPkeyAudioEndpointFormFactor, "AudioEndpoint FormFactor");

        Assert.Equal((int)expected, endpoint.HardwareDetails.FormFactorCode);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCapturePhysicalSpeakersFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        uint expected = ReadUInt32OrSkip(store, SdkPkeyAudioEndpointPhysicalSpeakers, "AudioEndpoint PhysicalSpeakers");

        Assert.Equal(expected, endpoint.HardwareDetails.PhysicalSpeakersMask);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureFullRangeSpeakersFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        uint expected = ReadUInt32OrSkip(store, SdkPkeyAudioEndpointFullRangeSpeakers, "AudioEndpoint FullRangeSpeakers");

        Assert.Equal(expected, endpoint.HardwareDetails.FullRangeSpeakersMask);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureEndpointGuidFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        string expected = ReadStringOrGuidOrSkip(store, SdkPkeyAudioEndpointGuid, "AudioEndpoint GUID");

        Assert.Equal(expected, endpoint.HardwareDetails.EndpointGuid);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureEventDrivenModeFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        bool expected = ReadBooleanOrSkip(store, SdkPkeyAudioEndpointSupportsEventDrivenMode, "AudioEndpoint SupportsEventDrivenMode");

        Assert.Equal(expected, endpoint.HardwareDetails.SupportsEventDrivenMode);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureJackSubTypeFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        string expected = ReadStringOrGuidOrSkip(store, SdkPkeyAudioEndpointJackSubType, "AudioEndpoint JackSubType");

        Assert.Equal(expected, endpoint.HardwareDetails.JackSubType);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureDeviceFormatSummaryFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        string expected = ReadBlobSummaryOrSkip(store, SdkPkeyAudioEngineDeviceFormat, "AudioEngine DeviceFormat");

        Assert.Equal(expected, endpoint.HardwareDetails.DeviceFormatSummary);
    }

    [SkippableFact]
    public void GetCurrentProfile_ShouldCarryPlaybackHardwareDetailsIntoTargetDevice()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo defaultPlayback = GetActiveDefaultPlaybackDevice(controller);
        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        HardwareTestHelpers.SkipIfNoActiveDevice(defaultPlayback, "playback");
        Skip.If(!profile.Playback.TargetDevice.IsEndpointEnabled, "Skipping because the playback target device was not captured.");

        Assert.Equal(defaultPlayback.HardwareDetails.FormFactorCode, profile.Playback.TargetDevice.HardwareDetails.FormFactorCode);
        Assert.Equal(defaultPlayback.HardwareDetails.PhysicalSpeakersMask, profile.Playback.TargetDevice.HardwareDetails.PhysicalSpeakersMask);
        Assert.Equal(defaultPlayback.HardwareDetails.FullRangeSpeakersMask, profile.Playback.TargetDevice.HardwareDetails.FullRangeSpeakersMask);
        Assert.Equal(defaultPlayback.HardwareDetails.EndpointGuid, profile.Playback.TargetDevice.HardwareDetails.EndpointGuid);
        Assert.Equal(defaultPlayback.HardwareDetails.DeviceFormatSummary, profile.Playback.TargetDevice.HardwareDetails.DeviceFormatSummary);
        Assert.Equal(defaultPlayback.HardwareDetails.JackSubType, profile.Playback.TargetDevice.HardwareDetails.JackSubType);
    }

    private static AudioEndpointInfo GetActiveDefaultPlaybackDevice(WindowsAudioController controller)
    {
        AudioEndpointInfo endpoint = HardwareTestHelpers.RunOrSkip(
            controller.GetDefaultPlaybackDevice,
            "default playback device read");

        HardwareTestHelpers.SkipIfNoActiveDevice(endpoint, "playback");
        return endpoint;
    }

    private static IPropertyStore OpenReadOnlyPropertyStore(AudioEndpointInfo endpoint)
    {
        IMMDevice device = CoreAudioUtilities.GetDeviceById(endpoint.DeviceId);
        int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
        Skip.If(hr < 0 || store == null, "Skipping because the endpoint property store could not be opened.");
        return store;
    }

    private static string ReadStringOrSkip(IPropertyStore store, PROPERTYKEY key, string featureName)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            Skip.If(hr < 0, $"Skipping because {featureName} is not exposed by this endpoint.");
            string result = value.GetStringOrStringList();
            Skip.If(string.IsNullOrWhiteSpace(result), $"Skipping because {featureName} is empty on this endpoint.");
            return result;
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    private static string ReadStringOrGuidOrSkip(IPropertyStore store, PROPERTYKEY key, string featureName)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            Skip.If(hr < 0, $"Skipping because {featureName} is not exposed by this endpoint.");
            string result = value.vt == CoreAudioConstants.VT_CLSID
                ? value.GetGuidString()
                : value.GetStringOrStringList();
            Skip.If(string.IsNullOrWhiteSpace(result), $"Skipping because {featureName} is empty on this endpoint.");
            return result;
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    private static uint ReadUInt32OrSkip(IPropertyStore store, PROPERTYKEY key, string featureName)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            Skip.If(hr < 0 || value.vt != CoreAudioConstants.VT_UI4, $"Skipping because {featureName} is not exposed as VT_UI4 by this endpoint.");
            return value.GetUInt32();
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    private static bool ReadBooleanOrSkip(IPropertyStore store, PROPERTYKEY key, string featureName)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            Skip.If(hr < 0, $"Skipping because {featureName} is not exposed by this endpoint.");
            Skip.If(value.vt is not (CoreAudioConstants.VT_BOOL or CoreAudioConstants.VT_UI4), $"Skipping because {featureName} is not exposed as a boolean-compatible type by this endpoint.");
            return value.GetBoolean();
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    private static string ReadBlobSummaryOrSkip(IPropertyStore store, PROPERTYKEY key, string featureName)
    {
        string result = CoreAudioUtilities.ReadBlobSummaryProperty(store, key);
        Skip.If(string.IsNullOrWhiteSpace(result), $"Skipping because {featureName} is not exposed by this endpoint.");
        return result;
    }
}
