namespace WindowsAudioWrapper.Internal.CoreAudio;

using System.Runtime.InteropServices;

internal static partial class CoreAudioConstants
{
    internal const int CLSCTX_ALL = 0x17;
    internal const int STGM_READ = 0;
    internal const int DEVICE_STATE_ACTIVE = 0x00000001;
    internal const int DEVICE_STATE_DISABLED = 0x00000002;
    internal const int DEVICE_STATE_NOTPRESENT = 0x00000004;
    internal const int DEVICE_STATE_UNPLUGGED = 0x00000008;
    internal const ushort VT_BOOL = 11;
    internal const ushort VT_UI4 = 19;
    internal const ushort VT_LPWSTR = 31;
    internal const ushort VT_CLSID = 72;
    internal const ushort VT_VECTOR = 0x1000;
    internal const int AUDCLNT_SHAREMODE_SHARED = 0;
    internal static readonly int AUDCLNT_E_EFFECT_NOT_AVAILABLE = unchecked((int)0x88890041);
    internal static readonly int AUDCLNT_E_EFFECT_STATE_READ_ONLY = unchecked((int)0x88890042);

    internal static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    internal static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    internal static readonly Guid IID_IAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
    internal static readonly Guid IID_IAudioEffectsManager = new("4460B3AE-4B44-4527-8676-7548A8ACD260");

    internal static readonly Guid GUID_PnPDeviceProperties = new("A45C254E-DF1C-4EFD-8020-67D146A850E0");
    internal static readonly Guid GUID_AudioEndpointProperties = new("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E");
    internal static readonly Guid GUID_DriverProperties = new("A85B04C5-C7B4-4A6B-984F-46F5F3AE43C7");

    internal static readonly PROPERTYKEY PKEY_Device_FriendlyName = new(GUID_PnPDeviceProperties, 14);
    internal static readonly PROPERTYKEY PKEY_DeviceInterface_FriendlyName = new(new Guid("026E516E-B814-414B-83CD-856D6FEF4822"), 2);
    internal static readonly PROPERTYKEY PKEY_Device_ContainerId = new(new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), 2);
    
    // Windows Native PnP Property lookups
    internal static readonly PROPERTYKEY PKEY_Device_DeviceDesc = new(GUID_PnPDeviceProperties, 2);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_FormFactor = new(GUID_AudioEndpointProperties, 0);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_PhysicalSpeakers = new(GUID_AudioEndpointProperties, 3);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_GUID = new(GUID_AudioEndpointProperties, 4);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_Disable_SysFx = new(GUID_AudioEndpointProperties, 5);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_FullRangeSpeakers = new(GUID_AudioEndpointProperties, 6);
    internal static readonly PROPERTYKEY PKEY_AudioEngine_DeviceFormat = new(new Guid("F19F064D-082C-4E27-BC73-6882A1BB8E4C"), 0);

    // Native IPropertyStore keys for advanced hardware capability tracking
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_Supports_EventDriven_Mode = new(GUID_AudioEndpointProperties, 7);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_JackSubType = new(GUID_AudioEndpointProperties, 8);

    [LibraryImport("ole32.dll")]
    internal static partial int PropVariantClear(ref PROPVARIANT pvar);
}

/// <summary>Identifies the direction of an audio endpoint.</summary>
public enum EDataFlow
{
    /// <summary>Playback endpoints.</summary>
    eRender = 0,
    /// <summary>Recording endpoints.</summary>
    eCapture = 1,
    /// <summary>Playback and recording endpoints.</summary>
    eAll = 2
}

/// <summary>Identifies the role assigned to a default audio endpoint.</summary>
public enum ERole
{
    /// <summary>The console role.</summary>
    eConsole = 0,
    /// <summary>The multimedia role.</summary>
    eMultimedia = 1,
    /// <summary>The communications role.</summary>
    eCommunications = 2
}

/// <summary>
/// Describes whether a Windows audio effect is currently off or on.
/// </summary>
public enum AudioEffectState
{
    /// <summary>The effect is disabled.</summary>
    Off = 0,
    /// <summary>The effect is enabled.</summary>
    On = 1
}

/// <summary>Identifies a property in a Windows property store.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PROPERTYKEY
{
    /// <summary>The property set identifier.</summary>
    public Guid fmtid;
    /// <summary>The property identifier within the set.</summary>
    public uint pid;

    /// <summary>Initializes a property key from its set and property identifiers.</summary>
    /// <param name="fmtid">The property set identifier.</param>
    /// <param name="pid">The property identifier within the set.</param>
    public PROPERTYKEY(Guid fmtid, uint pid)
    {
        this.fmtid = fmtid;
        this.pid = pid;
    }
}

/// <summary>Holds a property value returned by a Windows property store.</summary>
[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct PROPVARIANT
{
    /// <summary>The variant type describing the stored value.</summary>
    [FieldOffset(0)]
    public ushort vt;
    [FieldOffset(2)]
    private ushort wReserved1;
    [FieldOffset(4)]
    private ushort wReserved2;
    [FieldOffset(6)]
    private ushort wReserved3;
    /// <summary>A pointer to a string or GUID value, depending on <see cref="vt"/>.</summary>
    [FieldOffset(8)]
    public IntPtr p;
    /// <summary>The number of elements in a vector value.</summary>
    [FieldOffset(8)]
    public uint blobSize;
    /// <summary>A pointer to the vector elements.</summary>
    [FieldOffset(16)]
    public IntPtr blobData;
    /// <summary>An unsigned integer value, when the variant type is VT_UI4.</summary>
    [FieldOffset(8)]
    public uint uintValue;
    /// <summary>A Boolean value, when the variant type is VT_BOOL.</summary>
    [FieldOffset(8)]
    public short boolValue;

    /// <summary>Returns a string value from a string PROPVARIANT.</summary>
    public readonly string GetString()
    {
        return vt == 31 && p != IntPtr.Zero ? Marshal.PtrToStringUni(p) ?? string.Empty : string.Empty;
    }

    /// <summary>Returns a semicolon-separated string from a string or string-list PROPVARIANT.</summary>
    public readonly string GetStringOrStringList()
    {
        if (vt == CoreAudioConstants.VT_LPWSTR)
        {
            return GetString();
        }

        if (vt == (CoreAudioConstants.VT_VECTOR | CoreAudioConstants.VT_LPWSTR) && blobData != IntPtr.Zero)
        {
            var values = new List<string>();
            for (uint index = 0; index < blobSize; index++)
            {
                IntPtr stringPointer = Marshal.ReadIntPtr(blobData, checked((int)(index * (uint)IntPtr.Size)));
                if (stringPointer != IntPtr.Zero)
                {
                    string? value = Marshal.PtrToStringUni(stringPointer);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        values.Add(value);
                    }
                }
            }

            return string.Join(";", values);
        }

        return string.Empty;
    }

    /// <summary>Returns a GUID value from a CLSID PROPVARIANT.</summary>
    public readonly Guid GetGuid()
    {
        if (vt == 72 && p != IntPtr.Zero)
        {
            return Marshal.PtrToStructure<Guid>(p);
        }
        return Guid.Empty;
    }

    /// <summary>Returns a display-safe GUID string from a CLSID PROPVARIANT.</summary>
    public readonly string GetGuidString()
    {
        Guid guid = GetGuid();
        return guid == Guid.Empty ? string.Empty : guid.ToString("D");
    }

    /// <summary>Returns a uint value from a UI4 PROPVARIANT.</summary>
    public readonly uint GetUInt32()
    {
        return vt == CoreAudioConstants.VT_UI4 ? uintValue : 0;
    }

    /// <summary>Returns a bool value from a BOOL or UI4 PROPVARIANT.</summary>
    public readonly bool GetBoolean()
    {
        return vt switch
        {
            CoreAudioConstants.VT_BOOL => boolValue != 0,
            CoreAudioConstants.VT_UI4 => uintValue != 0,
            _ => false
        };
    }
}

/// <summary>Describes the basic format of an audio stream.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct WAVEFORMATEX
{
    /// <summary>The audio format tag.</summary>
    public ushort wFormatTag;
    /// <summary>The number of audio channels.</summary>
    public ushort nChannels;
    /// <summary>The number of samples per second.</summary>
    public uint nSamplesPerSec;
    /// <summary>The average number of bytes per second.</summary>
    public uint nAvgBytesPerSec;
    /// <summary>The size of one audio block in bytes.</summary>
    public ushort nBlockAlign;
    /// <summary>The number of bits per sample.</summary>
    public ushort wBitsPerSample;
    /// <summary>The size in bytes of additional format information.</summary>
    public ushort cbSize;
}

/// <summary>Describes a multichannel audio format with extended format fields.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct WAVEFORMATEXTENSIBLE
{
    /// <summary>The basic audio format information.</summary>
    public WAVEFORMATEX Format;
    /// <summary>The number of valid bits in each sample.</summary>
    public ushort wValidBitsPerSample;
    /// <summary>The channel positions present in the stream.</summary>
    public uint dwChannelMask;
    /// <summary>The format subtype identifier.</summary>
    public Guid SubFormat;
}

/// <summary>
/// Describes a Windows audio effect and its current mutable state.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct AUDIO_EFFECT
{
    /// <summary>The effect identifier.</summary>
    public Guid id;
    /// <summary>Whether the effect state can be changed.</summary>
    [MarshalAs(UnmanagedType.Bool)]
    public bool canSetState;
    /// <summary>The current effect state.</summary>
    public AudioEffectState state;
}

/// <summary>Enumerates audio endpoints and tracks default endpoints.</summary>
[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceEnumerator
{
    /// <summary>Gets endpoints matching a direction and device state mask.</summary>
    [PreserveSig]
    int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);

    /// <summary>Gets the default endpoint for a direction and role.</summary>
    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

    /// <summary>Gets an endpoint by its device identifier.</summary>
    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

    /// <summary>Registers a client for endpoint notifications.</summary>
    [PreserveSig]
    int RegisterEndpointNotificationCallback(IntPtr pClient);

    /// <summary>Unregisters a client from endpoint notifications.</summary>
    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IntPtr pClient);
}

/// <summary>Provides indexed access to a collection of audio endpoints.</summary>
[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceCollection
{
    /// <summary>Gets the number of endpoints in the collection.</summary>
    [PreserveSig]
    int GetCount(out uint pcDevices);

    /// <summary>Gets an endpoint by its index in the collection.</summary>
    [PreserveSig]
    int Item(uint nDevice, out IMMDevice ppDevice);
}

/// <summary>Represents an audio endpoint device.</summary>
[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice
{
    /// <summary>Activates a requested interface on the endpoint.</summary>
    [PreserveSig]
    int Activate(in Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.Interface)] out object ppInterface);

    /// <summary>Opens the endpoint's property store with the requested access.</summary>
    [PreserveSig]
    int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);

    /// <summary>Gets the endpoint's device identifier.</summary>
    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

    /// <summary>Gets the endpoint's current device state.</summary>
    [PreserveSig]
    int GetState(out int pdwState);
}

/// <summary>Reads and writes named values in a Windows property store.</summary>
[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPropertyStore
{
    /// <summary>Gets the number of properties in the store.</summary>
    [PreserveSig]
    int GetCount(out uint cProps);

    /// <summary>Gets a property key by index.</summary>
    [PreserveSig]
    int GetAt(uint iProp, out PROPERTYKEY pkey);

    /// <summary>Gets the value associated with a property key.</summary>
    [PreserveSig]
    int GetValue(in PROPERTYKEY key, out PROPVARIANT pv);

    /// <summary>Sets the value associated with a property key.</summary>
    [PreserveSig]
    int SetValue(in PROPERTYKEY key, in PROPVARIANT propvar);

    /// <summary>Commits property changes to the store.</summary>
    [PreserveSig]
    int Commit();
}

/// <summary>Controls and reads the volume and mute state of an audio endpoint.</summary>
[ComImport]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioEndpointVolume
{
    /// <summary>Registers a callback for endpoint volume changes.</summary>
    [PreserveSig]
    int RegisterControlChangeNotify(IntPtr pNotify);

    /// <summary>Unregisters a callback for endpoint volume changes.</summary>
    [PreserveSig]
    int UnregisterControlChangeNotify(IntPtr pNotify);

    /// <summary>Gets the number of endpoint channels.</summary>
    [PreserveSig]
    int GetChannelCount(out uint pnChannelCount);

    /// <summary>Sets the master volume level in decibels.</summary>
    [PreserveSig]
    int SetMasterVolumeLevel(float fLevelDB, in Guid pguidEventContext);

    /// <summary>Sets the master volume level on a normalized scale.</summary>
    [PreserveSig]
    int SetMasterVolumeLevelScalar(float fLevel, in Guid pguidEventContext);

    /// <summary>Gets the master volume level in decibels.</summary>
    [PreserveSig]
    int GetMasterVolumeLevel(out float pfLevelDB);

    /// <summary>Gets the master volume level on a normalized scale.</summary>
    [PreserveSig]
    int GetMasterVolumeLevelScalar(out float pfLevel);

    /// <summary>Sets a channel volume level in decibels.</summary>
    [PreserveSig]
    int SetChannelVolumeLevel(uint nChannel, float fLevelDB, in Guid pguidEventContext);

    /// <summary>Sets a channel volume level on a normalized scale.</summary>
    [PreserveSig]
    int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, in Guid pguidEventContext);

    /// <summary>Gets a channel volume level in decibels.</summary>
    [PreserveSig]
    int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

    /// <summary>Gets a channel volume level on a normalized scale.</summary>
    [PreserveSig]
    int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

    /// <summary>Sets the endpoint mute state.</summary>
    [PreserveSig]
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, in Guid pguidEventContext);

    /// <summary>Gets the endpoint mute state.</summary>
    [PreserveSig]
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);

    /// <summary>Gets the current volume step and total step count.</summary>
    [PreserveSig]
    int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

    /// <summary>Increases the endpoint volume by one step.</summary>
    [PreserveSig]
    int VolumeStepUp(in Guid pguidEventContext);

    /// <summary>Decreases the endpoint volume by one step.</summary>
    [PreserveSig]
    int VolumeStepDown(in Guid pguidEventContext);

    /// <summary>Gets the endpoint's volume control hardware support flags.</summary>
    [PreserveSig]
    int QueryHardwareSupport(out uint pdwHardwareSupportMask);

    /// <summary>Gets the minimum, maximum, and increment of the volume range in decibels.</summary>
    [PreserveSig]
    int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
}

/// <summary>Initializes and manages an audio stream for an endpoint.</summary>
[ComImport]
[Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioClient
{
    /// <summary>Initializes an audio stream with the requested format and buffering settings.</summary>
    [PreserveSig]
    int Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, in Guid audioSessionGuid);

    /// <summary>Gets the stream buffer size in frames.</summary>
    [PreserveSig]
    int GetBufferSize(out uint pNumBufferFrames);

    /// <summary>Gets the stream latency in 100-nanosecond units.</summary>
    [PreserveSig]
    int GetStreamLatency(out long phnsLatency);

    /// <summary>Gets the number of frames currently queued in the buffer.</summary>
    [PreserveSig]
    int GetCurrentPadding(out uint pNumPaddingFrames);

    /// <summary>Checks whether the endpoint supports an audio format.</summary>
    [PreserveSig]
    int IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);

    /// <summary>Gets the endpoint's shared-mode mix format.</summary>
    [PreserveSig]
    int GetMixFormat(out IntPtr ppDeviceFormat);

    /// <summary>Gets the default and minimum endpoint device periods.</summary>
    [PreserveSig]
    int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);

    /// <summary>Starts the audio stream.</summary>
    [PreserveSig]
    int Start();

    /// <summary>Stops the audio stream.</summary>
    [PreserveSig]
    int Stop();

    /// <summary>Resets the audio stream.</summary>
    [PreserveSig]
    int Reset();

    /// <summary>Sets the event handle used for event-driven buffering.</summary>
    [PreserveSig]
    int SetEventHandle(IntPtr eventHandle);

    /// <summary>Gets a service interface for the audio stream.</summary>
    [PreserveSig]
    int GetService(in Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
}

/// <summary>Enumerates and controls effects attached to an audio stream.</summary>
[ComImport]
[Guid("4460B3AE-4B44-4527-8676-7548A8ACD260")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioEffectsManager
{
    /// <summary>Registers a notification callback for audio effect list or state changes.</summary>
    [PreserveSig]
    int RegisterAudioEffectsChangedNotificationCallback(IntPtr client);

    /// <summary>Unregisters a notification callback for audio effect list or state changes.</summary>
    [PreserveSig]
    int UnregisterAudioEffectsChangedNotificationCallback(IntPtr client);

    /// <summary>Gets the current list of audio effects for the associated audio stream.</summary>
    [PreserveSig]
    int GetAudioEffects(out IntPtr effects, out uint numEffects);

    /// <summary>Sets the state of a mutable audio effect.</summary>
    [PreserveSig]
    int SetAudioEffectState(Guid effectId, AudioEffectState state);
}
