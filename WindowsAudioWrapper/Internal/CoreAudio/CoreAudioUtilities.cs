namespace WindowsAudioWrapper.Internal.CoreAudio;

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using WindowsAudioWrapper.Models;

internal static class CoreAudioUtilities
{
    public static IMMDeviceEnumerator CreateEnumerator()
    {
        int hr = CoreAudioConstants.CoCreateInstance(
            in CoreAudioConstants.CLSID_MMDeviceEnumerator,
            IntPtr.Zero,
            CoreAudioConstants.CLSCTX_ALL,
            in CoreAudioConstants.IID_IMMDeviceEnumerator,
            out IntPtr enumeratorPtr);

        if (hr < 0 || enumeratorPtr == IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        var strategy = new StrategyBasedComWrappers();
        return (IMMDeviceEnumerator)strategy.GetOrCreateObjectForComInstance(enumeratorPtr, CreateObjectFlags.None);
    }

    public static int ToNativeDeviceState(AudioDeviceState states)
    {
        if (states == AudioDeviceState.Unknown)
        {
            return CoreAudioConstants.DEVICE_STATE_ACTIVE;
        }

        int native = 0;
        if (states.HasFlag(AudioDeviceState.Active)) native |= CoreAudioConstants.DEVICE_STATE_ACTIVE;
        if (states.HasFlag(AudioDeviceState.Disabled)) native |= CoreAudioConstants.DEVICE_STATE_DISABLED;
        if (states.HasFlag(AudioDeviceState.NotPresent)) native |= CoreAudioConstants.DEVICE_STATE_NOTPRESENT;
        if (states.HasFlag(AudioDeviceState.Unplugged)) native |= CoreAudioConstants.DEVICE_STATE_UNPLUGGED;
        return native;
    }

    public static AudioDeviceState FromNativeDeviceState(int state)
    {
        AudioDeviceState result = AudioDeviceState.Unknown;
        if ((state & CoreAudioConstants.DEVICE_STATE_ACTIVE) != 0) result |= AudioDeviceState.Active;
        if ((state & CoreAudioConstants.DEVICE_STATE_DISABLED) != 0) result |= AudioDeviceState.Disabled;
        if ((state & CoreAudioConstants.DEVICE_STATE_NOTPRESENT) != 0) result |= AudioDeviceState.NotPresent;
        if ((state & CoreAudioConstants.DEVICE_STATE_UNPLUGGED) != 0) result |= AudioDeviceState.Unplugged;
        return result;
    }

    public static EDataFlow ToNativeFlow(AudioFlow flow)
    {
        return flow switch
        {
            AudioFlow.Render => EDataFlow.eRender,
            AudioFlow.Capture => EDataFlow.eCapture,
            _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, "Expected Render or Capture.")
        };
    }

    public static AudioFlow FromNativeFlow(EDataFlow flow)
    {
        return flow switch
        {
            EDataFlow.eRender => AudioFlow.Render,
            EDataFlow.eCapture => AudioFlow.Capture,
            _ => AudioFlow.Unknown
        };
    }

    public static string ReadStringProperty(IPropertyStore store, PROPERTYKEY key)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            if (hr < 0) return string.Empty;
            return value.GetString();
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    public static string ReadGuidPropertyAsString(IPropertyStore store, PROPERTYKEY key)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            if (hr < 0) return string.Empty;
            
            Guid guid = value.GetGuid();
            return guid == Guid.Empty ? string.Empty : guid.ToString("D");
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    public static IMMDevice GetDeviceById(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        IMMDeviceEnumerator enumerator = CreateEnumerator();
        
        int hr = enumerator.GetDevice(deviceId, out IntPtr devicePtr);
        if (hr < 0 || devicePtr == IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        var strategy = new StrategyBasedComWrappers();
        return (IMMDevice)strategy.GetOrCreateObjectForComInstance(devicePtr, CreateObjectFlags.None);
    }

    public static IAudioEndpointVolume ActivateEndpointVolume(string deviceId)
    {
        IMMDevice device = GetDeviceById(deviceId);
        
        int hr = device.Activate(CoreAudioConstants.IID_IAudioEndpointVolume, CoreAudioConstants.CLSCTX_ALL, IntPtr.Zero, out IntPtr endpointVolumePtr);
        if (hr < 0 || endpointVolumePtr == IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        var strategy = new StrategyBasedComWrappers();
        return (IAudioEndpointVolume)strategy.GetOrCreateObjectForComInstance(endpointVolumePtr, CreateObjectFlags.None);
    }

    public static IAudioClient ActivateAudioClient(string deviceId)
    {
        IMMDevice device = GetDeviceById(deviceId);
        
        int hr = device.Activate(CoreAudioConstants.IID_IAudioClient, CoreAudioConstants.CLSCTX_ALL, IntPtr.Zero, out IntPtr audioClientPtr);
        if (hr < 0 || audioClientPtr == IntPtr.Zero)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        var strategy = new StrategyBasedComWrappers();
        return (IAudioClient)strategy.GetOrCreateObjectForComInstance(audioClientPtr, CreateObjectFlags.None);
    }

    public static string ChooseDisplayName(string friendlyName, string interfaceName)
    {
        if (!string.IsNullOrWhiteSpace(friendlyName) && !string.IsNullOrWhiteSpace(interfaceName) &&
            !friendlyName.Equals(interfaceName, StringComparison.OrdinalIgnoreCase))
        {
            return $"{friendlyName} ({interfaceName})";
        }

        return !string.IsNullOrWhiteSpace(friendlyName) ? friendlyName : interfaceName;
    }
}