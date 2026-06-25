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

    public static string ReadBlobSummaryProperty(IPropertyStore store, PROPERTYKEY key)
    {
        PROPVARIANT value = default;
        try
        {
            int hr = store.GetValue(in key, out value);
            if (hr < 0 || value.vt != 65 || value.blobSize == 0 || value.blobData == IntPtr.Zero)
            {
                return string.Empty;
            }

            if (value.blobSize >= Marshal.SizeOf<WAVEFORMATEX>())
            {
                var waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(value.blobData);
                return $"FormatTag={waveFormat.wFormatTag};Channels={waveFormat.nChannels};SampleRate={waveFormat.nSamplesPerSec};BitsPerSample={waveFormat.wBitsPerSample};BlockAlign={waveFormat.nBlockAlign};AvgBytesPerSec={waveFormat.nAvgBytesPerSec}";
            }

            return $"BlobSize={value.blobSize}";
        }
        finally
        {
            CoreAudioConstants.PropVariantClear(ref value);
        }
    }

    /// <summary>
    /// Builds a compact serializable summary string from a native WAVEFORMATEX pointer.
    /// </summary>
    public static string BuildWaveFormatSummary(IntPtr formatPointer)
    {
        if (formatPointer == IntPtr.Zero)
        {
            return string.Empty;
        }

        var waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(formatPointer);
        string summary = $"FormatTag={waveFormat.wFormatTag};Channels={waveFormat.nChannels};SampleRate={waveFormat.nSamplesPerSec};BitsPerSample={waveFormat.wBitsPerSample};BlockAlign={waveFormat.nBlockAlign};AvgBytesPerSec={waveFormat.nAvgBytesPerSec}";

        if (waveFormat.wFormatTag == 65534 && waveFormat.cbSize >= 22)
        {
            var extensibleFormat = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(formatPointer);
            summary += $";ChannelMask={extensibleFormat.dwChannelMask};SubFormat={extensibleFormat.SubFormat:D}";
        }

        return summary;
    }

    /// <summary>
    /// Reads a compact serializable spatial audio capability summary for a render endpoint.
    /// </summary>
    public static string ReadSpatialAudioFormatSummary(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return string.Empty;
        }

        try
        {
            ISpatialAudioClient spatialClient = ActivateSpatialAudioClient(deviceId);

            bool streamAvailable = spatialClient.IsSpatialAudioStreamAvailable(
                CoreAudioConstants.IID_ISpatialAudioObjectRenderStream,
                IntPtr.Zero) >= 0;

            uint maxDynamicObjectCount = 0;
            if (spatialClient.GetMaxDynamicObjectCount(out uint dynamicObjectCount) >= 0)
            {
                maxDynamicObjectCount = dynamicObjectCount;
            }

            AudioObjectType nativeStaticObjectTypeMask = AudioObjectType.None;
            if (spatialClient.GetNativeStaticObjectTypeMask(out AudioObjectType staticMask) >= 0)
            {
                nativeStaticObjectTypeMask = staticMask;
            }

            uint supportedFormatCount = 0;
            string preferredFormat = string.Empty;
            uint preferredFrameCount = 0;

            if (spatialClient.GetSupportedAudioObjectFormatEnumerator(out IAudioFormatEnumerator enumerator) >= 0 &&
                enumerator != null &&
                enumerator.GetCount(out supportedFormatCount) >= 0 &&
                supportedFormatCount > 0 &&
                enumerator.GetFormat(0, out IntPtr formatPointer) >= 0 &&
                formatPointer != IntPtr.Zero)
            {
                preferredFormat = BuildWaveFormatSummary(formatPointer);
                if (spatialClient.GetMaxFrameCount(formatPointer, out uint frameCount) >= 0)
                {
                    preferredFrameCount = frameCount;
                }
            }

            return $"SpatialAudioClientAvailable=true;StreamAvailable={streamAvailable.ToString().ToLowerInvariant()};MaxDynamicObjectCount={maxDynamicObjectCount};NativeStaticObjectTypeMask={(uint)nativeStaticObjectTypeMask};SupportedFormatCount={supportedFormatCount};PreferredFrameCount={preferredFrameCount};PreferredFormat={preferredFormat}";
        }
        catch
        {
            return string.Empty;
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

    /// <summary>
    /// Activates the Windows spatial audio client for an endpoint device id.
    /// </summary>
    public static ISpatialAudioClient ActivateSpatialAudioClient(string deviceId)
    {
        IMMDevice device = GetDeviceById(deviceId);
        int hr = device.Activate(CoreAudioConstants.IID_ISpatialAudioClient, CoreAudioConstants.CLSCTX_ALL, IntPtr.Zero, out object spatialAudioClientObj);
        if (hr < 0 || spatialAudioClientObj == null)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return (ISpatialAudioClient)(spatialAudioClientObj ?? throw new COMException("Activated ISpatialAudioClient object was null."));
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
