namespace WindowsAudioWrapper.Providers;

using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;

internal sealed class AudioDeviceProvider : IAudioDeviceProvider
{
    public IReadOnlyList<AudioEndpointInfo> GetPlaybackDevices(AudioDeviceState states)
    {
        return EnumerateDevices(AudioFlow.Render, states);
    }

    public IReadOnlyList<AudioEndpointInfo> GetRecordingDevices(AudioDeviceState states)
    {
        return EnumerateDevices(AudioFlow.Capture, states);
    }

    public AudioEndpointInfo ResolveEndpoint(AudioEndpointReference endpoint, AudioFlow expectedFlow)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        IReadOnlyList<AudioEndpointInfo> devices = expectedFlow switch
        {
            AudioFlow.Render => GetPlaybackDevices(AudioDeviceState.All),
            AudioFlow.Capture => GetRecordingDevices(AudioDeviceState.All),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedFlow), expectedFlow, "Expected Render or Capture.")
        };

        AudioEndpointInfo? match = null;

        // 1. Try exact DeviceId match first (Standard path)
        if (!string.IsNullOrWhiteSpace(endpoint.DeviceId))
        {
            match = devices.FirstOrDefault(device =>
                device.DeviceId.Equals(endpoint.DeviceId, StringComparison.OrdinalIgnoreCase));
        }

        // 2. Fallback: Match via physical ContainerId + Flow (Handles USB port/hub swaps!)
        if (match is null && !string.IsNullOrWhiteSpace(endpoint.ContainerId))
        {
            match = devices.FirstOrDefault(device =>
                device.ContainerId.Equals(endpoint.ContainerId, StringComparison.OrdinalIgnoreCase) 
                && device.Flow == expectedFlow);
        }

        // 3. Last resort Fallback: Match via text FriendlyName
        if (match is null && !string.IsNullOrWhiteSpace(endpoint.FriendlyName))
        {
            match = devices.FirstOrDefault(device =>
                device.FriendlyName.Equals(endpoint.FriendlyName, StringComparison.OrdinalIgnoreCase));
        }

        // Return the matched device or create a placeholder marked as NotPresent
        return match ?? new AudioEndpointInfo
        {
            DeviceId = endpoint.DeviceId,
            ContainerId = endpoint.ContainerId,
            FriendlyName = endpoint.FriendlyName,
            FullName = endpoint.FullName,
            Flow = expectedFlow,
            State = AudioDeviceState.NotPresent,
            Capabilities = new AudioEndpointCapabilities
            {
                IsDefaultDeviceSupported = false,
                IsDefaultCommunicationsDeviceSupported = false,
                IsVolumeSupported = false,
                IsMuteSupported = false
            }
        };
    }   
    private static IReadOnlyList<AudioEndpointInfo> EnumerateDevices(AudioFlow flow, AudioDeviceState states)
    {
        List<AudioEndpointInfo> devices = new();
        IMMDeviceEnumerator enumerator = CoreAudioUtilities.CreateEnumerator();
        EDataFlow nativeFlow = CoreAudioUtilities.ToNativeFlow(flow);
        int nativeStates = CoreAudioUtilities.ToNativeDeviceState(states);

        string defaultDeviceId = GetDefaultDeviceIdOrEmpty(enumerator, nativeFlow, ERole.eMultimedia);
        string defaultCommunicationsDeviceId = GetDefaultDeviceIdOrEmpty(enumerator, nativeFlow, ERole.eCommunications);

        int hr = enumerator.EnumAudioEndpoints(nativeFlow, nativeStates, out IMMDeviceCollection collection);
        if (hr < 0 || collection == null)
        {
            return devices;
        }

        collection.GetCount(out uint count);
        for (uint index = 0; index < count; index++)
        {
            hr = collection.Item(index, out IMMDevice device);
            if (hr >= 0 && device != null)
            {
                devices.Add(CreateEndpointInfo(device, flow, defaultDeviceId, defaultCommunicationsDeviceId));
            }
        }

        return devices;
    }

    internal static AudioEndpointInfo CreateEndpointInfo(IMMDevice device, AudioFlow flow, string defaultDeviceId = "", string defaultCommunicationsDeviceId = "")
    {
        device.GetId(out string deviceId);
        device.GetState(out int nativeState);

        string friendlyName = string.Empty;
        string interfaceFriendlyName = string.Empty;
        string containerId = string.Empty;

        int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
        if (hr >= 0 && store != null)
        {
            friendlyName = CoreAudioUtilities.ReadStringProperty(store, CoreAudioConstants.PKEY_Device_FriendlyName);
            interfaceFriendlyName = CoreAudioUtilities.ReadStringProperty(store, CoreAudioConstants.PKEY_DeviceInterface_FriendlyName);
            containerId = CoreAudioUtilities.ReadGuidPropertyAsString(store, CoreAudioConstants.PKEY_Device_ContainerId);
        }

        AudioDeviceState state = CoreAudioUtilities.FromNativeDeviceState(nativeState);
        AudioEndpointInfo endpoint = new()
        {
            DeviceId = deviceId,
            ContainerId = containerId,
            FriendlyName = friendlyName,
            FullName = CoreAudioUtilities.ChooseDisplayName(friendlyName, interfaceFriendlyName),
            Flow = flow,
            State = state,
            IsDefaultDevice = deviceId.Equals(defaultDeviceId, StringComparison.OrdinalIgnoreCase),
            IsDefaultCommunicationsDevice = deviceId.Equals(defaultCommunicationsDeviceId, StringComparison.OrdinalIgnoreCase),
            Capabilities = new AudioEndpointCapabilities
            {
                IsDefaultDeviceSupported = true,
                IsDefaultCommunicationsDeviceSupported = true,
                IsVolumeSupported = state.HasFlag(AudioDeviceState.Active),
                IsMuteSupported = state.HasFlag(AudioDeviceState.Active),
                IsFormatReadSupported = true,
                IsFormatSetSupported = true,
                IsAudioEnhancementsReadSupported = true,
                IsAudioEnhancementsSetSupported = true
            }
        };

        if (state.HasFlag(AudioDeviceState.Active))
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
        catch (Exception)
        {
            endpoint.Capabilities.IsVolumeSupported = false;
            endpoint.Capabilities.IsMuteSupported = false;
        }
    }

    private static string GetDefaultDeviceIdOrEmpty(IMMDeviceEnumerator enumerator, EDataFlow flow, ERole role)
    {
        try
        {
            int hr = enumerator.GetDefaultAudioEndpoint(flow, role, out IMMDevice device);
            if (hr < 0 || device == null)
            {
                return string.Empty;
            }

            device.GetId(out string id);
            return id;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}