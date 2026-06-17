namespace WindowsAudioWrapper.Internal.CoreAudio;

using System.Runtime.InteropServices;
using WindowsAudioWrapper.Models;

internal static class CoreAudioUtilities
{
    public static CoreAudioInterop.IMMDeviceEnumerator CreateEnumerator()
    {
        Type? enumeratorType = Type.GetTypeFromCLSID(CoreAudioInterop.CLSID_MMDeviceEnumerator);
        if (enumeratorType is null)
        {
            throw new COMException("Unable to resolve the MMDeviceEnumerator COM class.");
        }

        object? enumerator = Activator.CreateInstance(enumeratorType);
        if (enumerator is not CoreAudioInterop.IMMDeviceEnumerator deviceEnumerator)
        {
            throw new COMException("Unable to create the MMDeviceEnumerator COM object.");
        }

        return deviceEnumerator;
    }

    public static int ToNativeDeviceState(AudioDeviceState states)
    {
        if (states == AudioDeviceState.Unknown)
        {
            return CoreAudioInterop.DEVICE_STATE_ACTIVE;
        }

        int native = 0;
        if (states.HasFlag(AudioDeviceState.Active)) native |= CoreAudioInterop.DEVICE_STATE_ACTIVE;
        if (states.HasFlag(AudioDeviceState.Disabled)) native |= CoreAudioInterop.DEVICE_STATE_DISABLED;
        if (states.HasFlag(AudioDeviceState.NotPresent)) native |= CoreAudioInterop.DEVICE_STATE_NOTPRESENT;
        if (states.HasFlag(AudioDeviceState.Unplugged)) native |= CoreAudioInterop.DEVICE_STATE_UNPLUGGED;
        return native;
    }

    public static AudioDeviceState FromNativeDeviceState(int state)
    {
        AudioDeviceState result = AudioDeviceState.Unknown;
        if ((state & CoreAudioInterop.DEVICE_STATE_ACTIVE) != 0) result |= AudioDeviceState.Active;
        if ((state & CoreAudioInterop.DEVICE_STATE_DISABLED) != 0) result |= AudioDeviceState.Disabled;
        if ((state & CoreAudioInterop.DEVICE_STATE_NOTPRESENT) != 0) result |= AudioDeviceState.NotPresent;
        if ((state & CoreAudioInterop.DEVICE_STATE_UNPLUGGED) != 0) result |= AudioDeviceState.Unplugged;
        return result;
    }

    public static CoreAudioInterop.EDataFlow ToNativeFlow(AudioFlow flow)
    {
        return flow switch
        {
            AudioFlow.Render => CoreAudioInterop.EDataFlow.eRender,
            AudioFlow.Capture => CoreAudioInterop.EDataFlow.eCapture,
            _ => throw new ArgumentOutOfRangeException(nameof(flow), flow, "Expected Render or Capture.")
        };
    }

    public static AudioFlow FromNativeFlow(CoreAudioInterop.EDataFlow flow)
    {
        return flow switch
        {
            CoreAudioInterop.EDataFlow.eRender => AudioFlow.Render,
            CoreAudioInterop.EDataFlow.eCapture => AudioFlow.Capture,
            _ => AudioFlow.Unknown
        };
    }

    public static string ReadStringProperty(CoreAudioInterop.IPropertyStore store, CoreAudioInterop.PROPERTYKEY key)
    {
        CoreAudioInterop.PROPVARIANT value = default;
        try
        {
            store.GetValue(ref key, out value);
            return value.GetString();
        }
        catch (COMException)
        {
            return string.Empty;
        }
        finally
        {
            try { CoreAudioInterop.PropVariantClear(ref value); } catch { }
        }
    }

    public static string ReadGuidPropertyAsString(CoreAudioInterop.IPropertyStore store, CoreAudioInterop.PROPERTYKEY key)
    {
        CoreAudioInterop.PROPVARIANT value = default;
        try
        {
            store.GetValue(ref key, out value);
            Guid guid = value.GetGuid();
            return guid == Guid.Empty ? string.Empty : guid.ToString("D");
        }
        catch (COMException)
        {
            return string.Empty;
        }
        finally
        {
            try { CoreAudioInterop.PropVariantClear(ref value); } catch { }
        }
    }

    public static CoreAudioInterop.IMMDevice GetDeviceById(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        CoreAudioInterop.IMMDeviceEnumerator enumerator = CreateEnumerator();
        enumerator.GetDevice(deviceId, out CoreAudioInterop.IMMDevice device);
        return device;
    }

    public static CoreAudioInterop.IAudioEndpointVolume ActivateEndpointVolume(string deviceId)
    {
        CoreAudioInterop.IMMDevice device = GetDeviceById(deviceId);
        Guid iid = CoreAudioInterop.IID_IAudioEndpointVolume;
        device.Activate(ref iid, CoreAudioInterop.CLSCTX_ALL, IntPtr.Zero, out object endpointVolumeObject);
        return (CoreAudioInterop.IAudioEndpointVolume)endpointVolumeObject;
    }

    public static CoreAudioInterop.IAudioClient ActivateAudioClient(string deviceId)
    {
        CoreAudioInterop.IMMDevice device = GetDeviceById(deviceId);
        Guid iid = CoreAudioInterop.IID_IAudioClient;
        device.Activate(ref iid, CoreAudioInterop.CLSCTX_ALL, IntPtr.Zero, out object audioClientObject);
        return (CoreAudioInterop.IAudioClient)audioClientObject;
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
