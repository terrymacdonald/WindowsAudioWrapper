using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;
using Xunit;

namespace WindowsAudioWrapper.Tests;

[Collection(AudioHardwareCollection.Name)]
public sealed class HardwareDetailsTests
{
    private static readonly PROPERTYKEY SdkPkeyDeviceHardwareIds = new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 3);
    private static readonly PROPERTYKEY SdkPkeyDeviceInstanceId = new(new Guid("78C34FC8-104A-4ACA-9EA4-524D52996E57"), 256);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointFormFactor = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 0);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointSupportsEventDrivenMode = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 7);
    private static readonly PROPERTYKEY SdkPkeyAudioEndpointJackSubType = new(new Guid("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E"), 8);

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
                HardwareId = "HDAUDIO\\FUNC_01",
                DriverVersion = "1.2.3.4",
                EndpointAssociationGuid = Guid.NewGuid().ToString("D"),
                FormFactorCode = 3,
                SupportsEventDrivenMode = true,
                JackSubType = Guid.NewGuid().ToString("D"),
                SpatialAudioFormat = Guid.NewGuid().ToString("D"),
                DeviceInstanceId = "SWD\\MMDEVAPI\\{test-device}"
            }
        };

        AudioEndpointReference reference = AudioEndpointReference.FromEndpointInfo(endpoint);

        Assert.Equal(endpoint.HardwareDetails.DeviceDescription, reference.HardwareDetails.DeviceDescription);
        Assert.Equal(endpoint.HardwareDetails.HardwareId, reference.HardwareDetails.HardwareId);
        Assert.Equal(endpoint.HardwareDetails.DriverVersion, reference.HardwareDetails.DriverVersion);
        Assert.Equal(endpoint.HardwareDetails.EndpointAssociationGuid, reference.HardwareDetails.EndpointAssociationGuid);
        Assert.Equal(endpoint.HardwareDetails.FormFactorCode, reference.HardwareDetails.FormFactorCode);
        Assert.Equal(endpoint.HardwareDetails.SupportsEventDrivenMode, reference.HardwareDetails.SupportsEventDrivenMode);
        Assert.Equal(endpoint.HardwareDetails.JackSubType, reference.HardwareDetails.JackSubType);
        Assert.Equal(endpoint.HardwareDetails.SpatialAudioFormat, reference.HardwareDetails.SpatialAudioFormat);
        Assert.Equal(endpoint.HardwareDetails.DeviceInstanceId, reference.HardwareDetails.DeviceInstanceId);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureDeviceInstanceIdFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        string expected = ReadStringOrSkip(store, SdkPkeyDeviceInstanceId, "DeviceInstanceId");

        Assert.Equal(expected, endpoint.HardwareDetails.DeviceInstanceId);
    }

    [SkippableFact]
    public void GetDefaultPlaybackDevice_ShouldCaptureHardwareIdsStringListFromSdkProperty()
    {
        using WindowsAudioController controller = new();
        AudioEndpointInfo endpoint = GetActiveDefaultPlaybackDevice(controller);
        IPropertyStore store = OpenReadOnlyPropertyStore(endpoint);

        string expected = ReadStringOrSkip(store, SdkPkeyDeviceHardwareIds, "HardwareIds");

        Assert.Equal(expected, endpoint.HardwareDetails.HardwareId);
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
    public void GetCurrentProfile_ShouldCarryPlaybackHardwareDetailsIntoTargetDevice()
    {
        using WindowsAudioController controller = new();

        AudioEndpointInfo defaultPlayback = GetActiveDefaultPlaybackDevice(controller);
        AudioProfile profile = HardwareTestHelpers.RunOrSkip(
            controller.GetCurrentProfile,
            "current audio profile capture");

        HardwareTestHelpers.SkipIfNoActiveDevice(defaultPlayback, "playback");
        Skip.If(!profile.Playback.TargetDevice.IsEndpointEnabled, "Skipping because the playback target device was not captured.");

        Assert.Equal(defaultPlayback.HardwareDetails.DeviceInstanceId, profile.Playback.TargetDevice.HardwareDetails.DeviceInstanceId);
        Assert.Equal(defaultPlayback.HardwareDetails.FormFactorCode, profile.Playback.TargetDevice.HardwareDetails.FormFactorCode);
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
}
