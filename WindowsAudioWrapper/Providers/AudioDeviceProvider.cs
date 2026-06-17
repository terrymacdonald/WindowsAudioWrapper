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

        if (!string.IsNullOrWhiteSpace(endpoint.DeviceId))
        {
            match = devices.FirstOrDefault(device =>
                device.DeviceId.Equals(endpoint.DeviceId, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null && !string.IsNullOrWhiteSpace(endpoint.FullName))
        {
            match = devices.FirstOrDefault(device =>
                device.FullName.Equals(endpoint.FullName, StringComparison.OrdinalIgnoreCase));
        }

        if (match is null && !string.IsNullOrWhiteSpace(endpoint.FriendlyName))
        {
            match = devices.FirstOrDefault(device =>
                device.FriendlyName.Equals(endpoint.FriendlyName, StringComparison.OrdinalIgnoreCase));
        }

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
        CoreAudioInterop.IMMDeviceEnumerator enumerator = CoreAudioUtilities.CreateEnumerator();
        CoreAudioInterop.EDataFlow nativeFlow = CoreAudioUtilities.ToNativeFlow(flow);
        int nativeStates = CoreAudioUtilities.ToNativeDeviceState(states);

        string defaultDeviceId = GetDefaultDeviceIdOrEmpty(enumerator, nativeFlow, CoreAudioInterop.ERole.eMultimedia);
        string defaultCommunicationsDeviceId = GetDefaultDeviceIdOrEmpty(enumerator, nativeFlow, CoreAudioInterop.ERole.eCommunications);

        enumerator.EnumAudioEndpoints(nativeFlow, nativeStates, out CoreAudioInterop.IMMDeviceCollection collection);
        collection.GetCount(out uint count);

        for (uint index = 0; index < count; index++)
        {
            collection.Item(index, out CoreAudioInterop.IMMDevice device);
            devices.Add(CreateEndpointInfo(device, flow, defaultDeviceId, defaultCommunicationsDeviceId));
        }

        return devices;
    }

    internal static AudioEndpointInfo CreateEndpointInfo(CoreAudioInterop.IMMDevice device, AudioFlow flow, string defaultDeviceId = "", string defaultCommunicationsDeviceId = "")
    {
        device.GetId(out string deviceId);
        device.GetState(out int nativeState);

        string friendlyName = string.Empty;
        string interfaceFriendlyName = string.Empty;
        string containerId = string.Empty;

        try
        {
            device.OpenPropertyStore(CoreAudioInterop.STGM_READ, out CoreAudioInterop.IPropertyStore store);
            friendlyName = CoreAudioUtilities.ReadStringProperty(store, CoreAudioInterop.PKEY_Device_FriendlyName);
            interfaceFriendlyName = CoreAudioUtilities.ReadStringProperty(store, CoreAudioInterop.PKEY_DeviceInterface_FriendlyName);
            containerId = CoreAudioUtilities.ReadGuidPropertyAsString(store, CoreAudioInterop.PKEY_Device_ContainerId);
        }
        catch (COMException)
        {
            // Keep endpoint identity even if metadata cannot be read.
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
                IsFormatReadSupported = false,
                IsFormatSetSupported = false,
                IsSpatialSoundReadSupported = false,
                IsSpatialSoundSetSupported = false,
                IsAudioEnhancementsReadSupported = false,
                IsAudioEnhancementsSetSupported = false,
                IsVoiceProcessingReadSupported = false,
                IsVoiceProcessingSetSupported = false
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
            CoreAudioInterop.IAudioEndpointVolume volume = CoreAudioUtilities.ActivateEndpointVolume(endpoint.DeviceId);
            volume.GetMasterVolumeLevelScalar(out float scalar);
            volume.GetMute(out bool muted);
            endpoint.VolumePercent = Math.Round((decimal)scalar * 100m, 2);
            endpoint.IsMuted = muted;
        }
        catch (Exception) when (IsRecoverableAudioException())
        {
            endpoint.Capabilities.IsVolumeSupported = false;
            endpoint.Capabilities.IsMuteSupported = false;
        }
    }

    private static string GetDefaultDeviceIdOrEmpty(CoreAudioInterop.IMMDeviceEnumerator enumerator, CoreAudioInterop.EDataFlow flow, CoreAudioInterop.ERole role)
    {
        try
        {
            enumerator.GetDefaultAudioEndpoint(flow, role, out CoreAudioInterop.IMMDevice device);
            device.GetId(out string id);
            return id;
        }
        catch (COMException)
        {
            return string.Empty;
        }
    }

    private static bool IsRecoverableAudioException()
    {
        return true;
    }
}
