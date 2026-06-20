namespace WindowsAudioWrapper.Providers;

using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Internal.PolicyConfig;
using WindowsAudioWrapper.Models;

internal sealed class DefaultAudioDeviceProvider : IDefaultAudioDeviceProvider
{
    public AudioEndpointInfo GetDefaultPlaybackDevice()
    {
        return GetDefaultDevice(AudioFlow.Render, ERole.eMultimedia);
    }

    public AudioEndpointInfo GetDefaultRecordingDevice()
    {
        return GetDefaultDevice(AudioFlow.Capture, ERole.eMultimedia);
    }

    public AudioEndpointInfo GetDefaultCommunicationsPlaybackDevice()
    {
        return GetDefaultDevice(AudioFlow.Render, ERole.eCommunications);
    }

    public AudioEndpointInfo GetDefaultCommunicationsRecordingDevice()
    {
        return GetDefaultDevice(AudioFlow.Capture, ERole.eCommunications);
    }

    public void SetDefaultPlaybackDevice(AudioEndpointReference endpoint)
    {
        SetDefaultDevice(endpoint, ERole.eConsole, ERole.eMultimedia);
    }

    public void SetDefaultRecordingDevice(AudioEndpointReference endpoint)
    {
        SetDefaultDevice(endpoint, ERole.eConsole, ERole.eMultimedia);
    }

    public void SetDefaultCommunicationsPlaybackDevice(AudioEndpointReference endpoint)
    {
        SetDefaultDevice(endpoint, ERole.eCommunications);
    }

    public void SetDefaultCommunicationsRecordingDevice(AudioEndpointReference endpoint)
    {
        SetDefaultDevice(endpoint, ERole.eCommunications);
    }

    private static AudioEndpointInfo GetDefaultDevice(AudioFlow flow, ERole role)
    {
        try
        {
            IMMDeviceEnumerator enumerator = CoreAudioUtilities.CreateEnumerator();
            int hr = enumerator.GetDefaultAudioEndpoint(CoreAudioUtilities.ToNativeFlow(flow), role, out IntPtr devicePtr);
            
            if (hr < 0 || devicePtr == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            try
            {
                var device = (IMMDevice)Marshal.GetObjectForIUnknown(devicePtr);
                device.GetId(out string defaultId);
                AudioEndpointInfo endpoint = AudioDeviceProvider.CreateEndpointInfo(device, flow, defaultId, role == ERole.eCommunications ? defaultId : string.Empty);
                
                if (role == ERole.eCommunications)
                {
                    endpoint.IsDefaultCommunicationsDevice = true;
                }
                else
                {
                    endpoint.IsDefaultDevice = true;
                }

                return endpoint;
            }
            finally
            {
                Marshal.Release(devicePtr);
            }
        }
        catch (Exception ex)
        {
            return new AudioEndpointInfo
            {
                Flow = flow,
                State = AudioDeviceState.NotPresent,
                Capabilities = new AudioEndpointCapabilities
                {
                    IsDefaultDeviceSupported = false,
                    IsDefaultCommunicationsDeviceSupported = false,
                    IsVolumeSupported = false,
                    IsMuteSupported = false
                },
                FullName = $"No default {flow} device ({ex.Message})"
            };
        }
    }

    private static void SetDefaultDevice(AudioEndpointReference endpoint, params ERole[] roles)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
        {
            throw new ArgumentException("Endpoint DeviceId is required to set a default audio device.", nameof(endpoint));
        }

        Type? policyConfigType = Type.GetTypeFromCLSID(PolicyConfigInterop.CLSID_PolicyConfigClient);
        if (policyConfigType is null)
        {
            throw new COMException("Unable to resolve the PolicyConfigClient COM class.");
        }

        object? policyConfigObject = Activator.CreateInstance(policyConfigType);
        if (policyConfigObject is not PolicyConfigInterop.IPolicyConfig policyConfig)
        {
            throw new COMException("Unable to create the PolicyConfigClient COM object.");
        }

        foreach (ERole role in roles)
        {
            int hr = policyConfig.SetDefaultEndpoint(endpoint.DeviceId, (int)role);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
    }
}