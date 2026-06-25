namespace WindowsAudioWrapper.Providers;

using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;

internal sealed class AudioDeviceProvider : IAudioDeviceProvider
{
    public IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states) => EnumerateDevices(AudioFlow.Render, states);
    public IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states) => EnumerateDevices(AudioFlow.Capture, states);

    public AudioEndpointInfo ResolveEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        IReadOnlyList<AudioEndpointInfo> devices = expectedFlow switch
        {
            AudioFlow.Render => GetPlaybackDevices(AudioDeviceState.All),
            AudioFlow.Capture => GetRecordingDevices(AudioDeviceState.All),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedFlow), expectedFlow, null)
        };

        AudioEndpointInfo? match = null;

        // Tier 1: Exact DeviceId Match
        if (!string.IsNullOrWhiteSpace(endpoint.DeviceId))
        {
            match = devices.FirstOrDefault(d => d.DeviceId.Equals(endpoint.DeviceId, StringComparison.OrdinalIgnoreCase));
        }

        // Tier 2: Physical ContainerId Match
        if (match is null && !string.IsNullOrWhiteSpace(endpoint.ContainerId))
        {
            match = devices.FirstOrDefault(d => d.ContainerId.Equals(endpoint.ContainerId, StringComparison.OrdinalIgnoreCase) && d.Flow == expectedFlow);
        }

        // Tier 3: Text-based Friendly Name
        if (match is null && !string.IsNullOrWhiteSpace(endpoint.FriendlyName))
        {
            match = devices.FirstOrDefault(d => d.FriendlyName.Equals(endpoint.FriendlyName, StringComparison.OrdinalIgnoreCase));
        }

        return match ?? new AudioEndpointInfo 
        { 
            DeviceId = endpoint.DeviceId ?? string.Empty,
            ContainerId = endpoint.ContainerId ?? string.Empty,
            FriendlyName = endpoint.FriendlyName ?? string.Empty,
            FullName = endpoint.FullName ?? string.Empty,
            Flow = expectedFlow,
            State = AudioDeviceState.NotPresent,
            Capabilities = new AudioEndpointCapabilities
            {
                IsDefaultDeviceSupported = false,
                IsDefaultCommunicationsDeviceSupported = false,
                IsVolumeSupported = false,
                IsMuteSupported = false,
                IsAudioEnhancementsReadSupported = false,
                IsAudioEnhancementsSetSupported = false
            }
        };
    }

    private static IReadOnlyList<AudioEndpointInfo> EnumerateDevices(AudioFlow flow, AudioDeviceState states)
    {
        List<AudioEndpointInfo> devices = new();
        IMMDeviceEnumerator enumerator = CoreAudioUtilities.CreateEnumerator();
        EDataFlow nativeFlow = CoreAudioUtilities.ToNativeFlow(flow);
        int nativeStates = CoreAudioUtilities.ToNativeDeviceState(states);

        int hr = enumerator.EnumAudioEndpoints(nativeFlow, nativeStates, out IMMDeviceCollection collection);
        if (hr < 0 || collection == null) return devices;

        string defaultDeviceId = string.Empty;
        string defaultCommsId = string.Empty;
        
        if (enumerator.GetDefaultAudioEndpoint(nativeFlow, ERole.eMultimedia, out IMMDevice defaultDevice) >= 0 && defaultDevice != null)
        {
            defaultDevice.GetId(out defaultDeviceId);
        }
        if (enumerator.GetDefaultAudioEndpoint(nativeFlow, ERole.eCommunications, out IMMDevice defaultCommsDevice) >= 0 && defaultCommsDevice != null)
        {
            defaultCommsDevice.GetId(out defaultCommsId);
        }

        collection.GetCount(out uint count);
        for (uint i = 0; i < count; i++)
        {
            if (collection.Item(i, out IMMDevice device) >= 0 && device != null)
            {
                devices.Add(CreateEndpointInfo(device, flow, defaultDeviceId, defaultCommsId));
            }
        }
        return devices;
    }

    internal static AudioEndpointInfo CreateEndpointInfo(IMMDevice device, AudioFlow flow, string defaultDeviceId = "", string defaultCommunicationsDeviceId = "")
    {
        device.GetId(out string deviceId);
        device.GetState(out int nativeState);

        var endpoint = new AudioEndpointInfo
        {
            DeviceId = deviceId,
            Flow = flow,
            State = CoreAudioUtilities.FromNativeDeviceState(nativeState),
            IsDefaultDevice = deviceId.Equals(defaultDeviceId, StringComparison.OrdinalIgnoreCase),
            IsDefaultCommunicationsDevice = deviceId.Equals(defaultCommunicationsDeviceId, StringComparison.OrdinalIgnoreCase)
        };

        int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
        if (hr >= 0 && store != null)
        {
            string friendlyName = TrySafeReadString(store, CoreAudioConstants.PKEY_Device_FriendlyName);
            string interfaceName = TrySafeReadString(store, CoreAudioConstants.PKEY_DeviceInterface_FriendlyName);
            
            endpoint.FriendlyName = friendlyName;
            endpoint.FullName = CoreAudioUtilities.ChooseDisplayName(friendlyName, interfaceName);
            endpoint.ContainerId = TrySafeReadGuid(store, CoreAudioConstants.PKEY_Device_ContainerId);

            // Extract and map all unmanaged property records securely
            endpoint.HardwareDetails = new HardwareDetails
            {
                DeviceDescription = TrySafeReadString(store, CoreAudioConstants.PKEY_Device_DeviceDesc),
                PhysicalSpeakersMask = TrySafeReadUInt32(store, CoreAudioConstants.PKEY_AudioEndpoint_PhysicalSpeakers),
                FullRangeSpeakersMask = TrySafeReadUInt32(store, CoreAudioConstants.PKEY_AudioEndpoint_FullRangeSpeakers),
                EndpointGuid = TrySafeReadStringOrGuid(store, CoreAudioConstants.PKEY_AudioEndpoint_GUID),
                DeviceFormatSummary = TrySafeReadBlobSummary(store, CoreAudioConstants.PKEY_AudioEngine_DeviceFormat),
                JackSubType = TrySafeReadStringOrGuid(store, CoreAudioConstants.PKEY_AudioEndpoint_JackSubType),
                SpatialAudioFormat = flow == AudioFlow.Render ? TrySafeReadSpatialAudioSummary(deviceId) : string.Empty,
                SupportsEventDrivenMode = TrySafeReadBoolean(store, CoreAudioConstants.PKEY_AudioEndpoint_Supports_EventDriven_Mode),
                FormFactorCode = (int)TrySafeReadUInt32(store, CoreAudioConstants.PKEY_AudioEndpoint_FormFactor)
            };

            PROPVARIANT propVar = default;
            if (store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out propVar) >= 0)
            {
                if (propVar.vt == 11) // VT_BOOL
                {
                    endpoint.AudioEnhancements.AreEnhancementsSupported = true;
                    endpoint.AudioEnhancements.DisableAllEnhancements = propVar.p != IntPtr.Zero; 
                    endpoint.Capabilities.IsAudioEnhancementsReadSupported = true;
                    endpoint.Capabilities.IsAudioEnhancementsSetSupported = true;
                }
                CoreAudioConstants.PropVariantClear(ref propVar);
            }
        }

        if (endpoint.State.HasFlag(AudioDeviceState.Active))
        {
            TryPopulateVolumeAndMute(endpoint);
        }

        return endpoint;
    }

    private static string TrySafeReadString(IPropertyStore store, PROPERTYKEY key)
    {
        try
        {
            return CoreAudioUtilities.ReadStringProperty(store, key);
        }
        catch
        {
            return string.Empty; // Fail-open baseline for software/virtual drivers lacking this key
        }
    }

    private static string TrySafeReadGuid(IPropertyStore store, PROPERTYKEY key)
    {
        try
        {
            return CoreAudioUtilities.ReadGuidPropertyAsString(store, key);
        }
        catch
        {
            return string.Empty; // Fail-open baseline for virtual drivers lacking static associations
        }
    }

    private static string TrySafeReadStringOrGuid(IPropertyStore store, PROPERTYKEY key)
    {
        try
        {
            return CoreAudioUtilities.ReadStringOrGuidProperty(store, key);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static uint TrySafeReadUInt32(IPropertyStore store, PROPERTYKEY key)
    {
        try
        {
            return CoreAudioUtilities.ReadUInt32Property(store, key);
        }
        catch
        {
            return 0;
        }
    }

    private static bool TrySafeReadBoolean(IPropertyStore store, PROPERTYKEY key)
    {
        try
        {
            return CoreAudioUtilities.ReadBooleanProperty(store, key);
        }
        catch
        {
            return false;
        }
    }

    private static string TrySafeReadBlobSummary(IPropertyStore store, PROPERTYKEY key)
    {
        try
        {
            return CoreAudioUtilities.ReadBlobSummaryProperty(store, key);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string TrySafeReadSpatialAudioSummary(string deviceId)
    {
        try
        {
            return CoreAudioUtilities.ReadSpatialAudioFormatSummary(deviceId);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void TryPopulateVolumeAndMute(AudioEndpointInfo endpoint)
    {
        try
        {
            IAudioEndpointVolume volume = CoreAudioUtilities.ActivateEndpointVolume(endpoint.DeviceId);
            int volumeHr = volume.GetMasterVolumeLevelScalar(out float scalar);
            if (volumeHr >= 0)
            {
                endpoint.VolumePercent = Math.Round((decimal)scalar * 100m, 2);
                endpoint.Capabilities.IsVolumeSupported = true;
            }

            int muteHr = volume.GetMute(out bool muted);
            if (muteHr >= 0)
            {
                endpoint.IsMuted = muted;
                endpoint.Capabilities.IsMuteSupported = true;
            }
        }
        catch
        {
            endpoint.Capabilities.IsVolumeSupported = false;
            endpoint.Capabilities.IsMuteSupported = false;
        }
    }
}
