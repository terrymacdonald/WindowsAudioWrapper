namespace WindowsAudioWrapper.Internal.CoreAudio;

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

/// <summary>
/// Custom unmanaged COM layout interface mapping to native IMMNotificationClient events.
/// Uses primitive flat parameters to protect source generators from nested type marshaling errors.
/// </summary>
[GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16)]
[Guid("79A74C4A-EDB3-47E8-B626-343A2A1A6690")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface INativeNotificationClient
{
    [PreserveSig]
    int OnDeviceStateChanged(string pwstrDeviceId, int dwNewState);

    [PreserveSig]
    int OnDeviceAdded(string pwstrDeviceId);

    [PreserveSig]
    int OnDeviceDeleted(string pwstrDeviceId);

    [PreserveSig]
    int OnDefaultDeviceChanged(int flow, int role, string pwstrDefaultDeviceId);

    [PreserveSig]
    int OnPropertyValueChanged(string pwstrDeviceId, IntPtr pkey);
}

/// <summary>
/// Internal event broker that translates unmanaged Windows Audio Service notifications into clean .NET events.
/// </summary>
[GeneratedComClass]
internal sealed partial class AudioNotificationRouter : INativeNotificationClient
{
    public event Action<string>? DeviceNotificationReceived;

    public int OnDeviceStateChanged(string pwstrDeviceId, int dwNewState)
    {
        // 0x00000001 maps to DEVICE_STATE_ACTIVE
        if (dwNewState == 1)
        {
            DeviceNotificationReceived?.Invoke(pwstrDeviceId);
        }
        return 0; // S_OK
    }

    public int OnDeviceAdded(string pwstrDeviceId)
    {
        DeviceNotificationReceived?.Invoke(pwstrDeviceId);
        return 0; // S_OK
    }

    public int OnDeviceDeleted(string pwstrDeviceId) => 0; // S_OK
    public int OnDefaultDeviceChanged(int flow, int role, string pwstrDefaultDeviceId) => 0; // S_OK
    public int OnPropertyValueChanged(string pwstrDeviceId, IntPtr pkey) => 0; // S_OK
}