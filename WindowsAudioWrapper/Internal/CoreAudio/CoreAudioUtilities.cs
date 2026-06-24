namespace WindowsAudioWrapper.Internal.CoreAudio;

using System.Runtime.InteropServices;
using WindowsAudioWrapper.Models;

internal static class CoreAudioUtilities
{
    public static IMMDeviceEnumerator CreateEnumerator()
    {
        Type? enumeratorType = Type.GetTypeFromCLSID(CoreAudioConstants.CLSID_MMDeviceEnumerator);
        if (enumeratorType is null)
        {
            throw new COMException("Unable to resolve the MMDeviceEnumerator COM class.");
        }

        object? enumerator = Activator.CreateInstance(enumeratorType);
        return (IMMDeviceEnumerator)(enumerator ?? throw new COMException("Failed to instantiate MMDeviceEnumerator."));
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

    public static string ReadStringProperty(IPropertyStore store, PROPERTYKEY key)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            if (hr < 0) return string.Empty;
            return value.GetStringOrStringList();
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
            
            return value.GetGuidString();
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    public static string ReadStringOrGuidProperty(IPropertyStore store, PROPERTYKEY key)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            if (hr < 0) return string.Empty;

            return value.vt switch
            {
                CoreAudioConstants.VT_CLSID => value.GetGuidString(),
                _ => value.GetStringOrStringList()
            };
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    public static uint ReadUInt32Property(IPropertyStore store, PROPERTYKEY key)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            if (hr < 0) return 0;
            return value.GetUInt32();
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    public static bool ReadBooleanProperty(IPropertyStore store, PROPERTYKEY key)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            if (hr < 0) return false;
            return value.GetBoolean();
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
        int hr = enumerator.GetDevice(deviceId, out IMMDevice device);
        if (hr < 0 || device == null)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return device ?? throw new COMException("The returned IMMDevice instance was null.");
    }

    public static IAudioEndpointVolume ActivateEndpointVolume(string deviceId)
    {
        IMMDevice device = GetDeviceById(deviceId);
        int hr = device.Activate(CoreAudioConstants.IID_IAudioEndpointVolume, CoreAudioConstants.CLSCTX_ALL, IntPtr.Zero, out object endpointVolumeObj);
        if (hr < 0 || endpointVolumeObj == null)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return (IAudioEndpointVolume)(endpointVolumeObj ?? throw new COMException("Activated IAudioEndpointVolume object was null."));
    }

    public static IAudioClient ActivateAudioClient(string deviceId)
    {
        IMMDevice device = GetDeviceById(deviceId);
        int hr = device.Activate(CoreAudioConstants.IID_IAudioClient, CoreAudioConstants.CLSCTX_ALL, IntPtr.Zero, out object audioClientObj);
        if (hr < 0 || audioClientObj == null)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return (IAudioClient)(audioClientObj ?? throw new COMException("Activated IAudioClient object was null."));
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
