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

        // Tier 3: HardwareId + Association GUID Match (Driver Resilience)
        if (match is null && endpoint.HardwareDetails != null && !string.IsNullOrWhiteSpace(endpoint.HardwareDetails.HardwareId))
        {
            match = devices.FirstOrDefault(d => 
                d.HardwareDetails.HardwareId.Equals(endpoint.HardwareDetails.HardwareId, StringComparison.OrdinalIgnoreCase) &&
                d.HardwareDetails.EndpointAssociationGuid.Equals(endpoint.HardwareDetails.EndpointAssociationGuid, StringComparison.OrdinalIgnoreCase));
        }

        // Tier 4: Text-based Friendly Name
        if (match is null && !string.IsNullOrWhiteSpace(endpoint.FriendlyName))
        {
            match = devices.FirstOrDefault(d => d.FriendlyName.Equals(endpoint.FriendlyName, StringComparison.OrdinalIgnoreCase));
        }

        return match ?? new AudioEndpointInfo { State = AudioDeviceState.NotPresent, Flow = expectedFlow };
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
            string friendlyName = CoreAudioUtilities.ReadStringProperty(store, CoreAudioConstants.PKEY_Device_FriendlyName);
            string interfaceName = CoreAudioUtilities.ReadStringProperty(store, CoreAudioConstants.PKEY_DeviceInterface_FriendlyName);
            
            endpoint.FriendlyName = friendlyName;
            endpoint.FullName = CoreAudioUtilities.ChooseDisplayName(friendlyName, interfaceName);
            endpoint.ContainerId = CoreAudioUtilities.ReadGuidPropertyAsString(store, CoreAudioConstants.PKEY_Device_ContainerId);

            endpoint.HardwareDetails = new HardwareDetails
            {
                DeviceDescription = CoreAudioUtilities.ReadStringProperty(store, CoreAudioConstants.PKEY_Device_DeviceDesc),
                HardwareId = CoreAudioUtilities.ReadStringProperty(store, CoreAudioConstants.PKEY_Device_HardwareIds),
                DriverVersion = CoreAudioUtilities.ReadStringProperty(store, CoreAudioConstants.PKEY_Device_DriverVersion),
                EndpointAssociationGuid = CoreAudioUtilities.ReadGuidPropertyAsString(store, CoreAudioConstants.PKEY_AudioEndpoint_Association)
            };

            PROPVARIANT propVar = default;
            if (store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out propVar) >= 0)
            {
                if (propVar.vt == 11) // VT_BOOL
                {
                    endpoint.AudioEnhancements.AreEnhancementsSupported = true;
                    endpoint.AudioEnhancements.DisableAllEnhancements = propVar.p != IntPtr.Zero; 
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

    private static void TryPopulateVolumeAndMute(AudioEndpointInfo endpoint)
    {
        try
        {
            IAudioEndpointVolume volume = CoreAudioUtilities.ActivateEndpointVolume(endpoint.DeviceId);
            volume.GetMasterVolumeLevelScalar(out float scalar);
            volume.GetMute(out bool muted);
            endpoint.VolumePercent = Math.Round((decimal)scalar * 100m, 2);
            endpoint.IsMuted = muted;
        }
        catch
        {
            endpoint.Capabilities.IsVolumeSupported = false;
            endpoint.Capabilities.IsMuteSupported = false;
        }
    }
}