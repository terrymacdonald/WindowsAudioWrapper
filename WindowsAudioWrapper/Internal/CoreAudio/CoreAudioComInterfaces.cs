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

    internal static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    internal static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    internal static readonly Guid IID_IAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");

    internal static readonly Guid GUID_PnPDeviceProperties = new("A45C254E-DF1C-4EFD-8020-67D146A850E0");
    internal static readonly Guid GUID_AudioEndpointProperties = new("1DA5D803-D492-4EDD-8C23-E0C0FFEE7F0E");
    internal static readonly Guid GUID_DriverProperties = new("A85B04C5-C7B4-4A6B-984F-46F5F3AE43C7");

    internal static readonly PROPERTYKEY PKEY_Device_FriendlyName = new(GUID_PnPDeviceProperties, 14);
    internal static readonly PROPERTYKEY PKEY_DeviceInterface_FriendlyName = new(new Guid("026E516E-B814-414B-83CD-856D6FEF4822"), 2);
    internal static readonly PROPERTYKEY PKEY_Device_ContainerId = new(new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), 2);
    
    // Windows Native PnP Property lookups
    internal static readonly PROPERTYKEY PKEY_Device_DeviceDesc = new(GUID_PnPDeviceProperties, 2);
    internal static readonly PROPERTYKEY PKEY_Device_HardwareIds = new(GUID_PnPDeviceProperties, 3);
    internal static readonly PROPERTYKEY PKEY_Device_DriverVersion = new(GUID_DriverProperties, 3);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_Association = new(GUID_AudioEndpointProperties, 2);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_Disable_SysFx = new(GUID_AudioEndpointProperties, 5);

    // Native IPropertyStore keys for advanced hardware capability tracking
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_Supports_EventDriven_Mode = new(GUID_AudioEndpointProperties, 3);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_JackSubType = new(new Guid("2A91DE60-C901-4A35-8C5E-3466378D570F"), 1);
    internal static readonly PROPERTYKEY PKEY_AudioEndpoint_Spatial = new(GUID_AudioEndpointProperties, 4);
    internal static readonly PROPERTYKEY PKEY_Device_InstanceId = new(new Guid("786505C7-3BEA-415F-862D-8B1826946917"), 2);
    internal static readonly PROPERTYKEY PKEY_Device_FormFactor = new(GUID_PnPDeviceProperties, 11);

    [LibraryImport("ole32.dll")]
    internal static partial int PropVariantClear(ref PROPVARIANT pvar);
}

public enum EDataFlow
{
    eRender = 0,
    eCapture = 1,
    eAll = 2
}

public enum ERole
{
    eConsole = 0,
    eMultimedia = 1,
    eCommunications = 2
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct PROPERTYKEY
{
    public Guid fmtid;
    public uint pid;

    public PROPERTYKEY(Guid fmtid, uint pid)
    {
        this.fmtid = fmtid;
        this.pid = pid;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct PROPVARIANT
{
    [FieldOffset(0)]
    public ushort vt;
    [FieldOffset(2)]
    private ushort wReserved1;
    [FieldOffset(4)]
    private ushort wReserved2;
    [FieldOffset(6)]
    private ushort wReserved3;
    [FieldOffset(8)]
    public IntPtr p;
    [FieldOffset(8)]
    public uint blobSize;
    [FieldOffset(16)]
    public IntPtr blobData;

    public readonly string GetString()
    {
        return vt == 31 && p != IntPtr.Zero ? Marshal.PtrToStringUni(p) ?? string.Empty : string.Empty;
    }

    public readonly Guid GetGuid()
    {
        if (vt == 72 && p != IntPtr.Zero)
        {
            return Marshal.PtrToStructure<Guid>(p);
        }
        return Guid.Empty;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct WAVEFORMATEX
{
    public ushort wFormatTag;
    public ushort nChannels;
    public uint nSamplesPerSec;
    public uint nAvgBytesPerSec;
    public ushort nBlockAlign;
    public ushort wBitsPerSample;
    public ushort cbSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct WAVEFORMATEXTENSIBLE
{
    public WAVEFORMATEX Format;
    public ushort wValidBitsPerSample;
    public uint dwChannelMask;
    public Guid SubFormat;
}

[ComImport]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceEnumerator
{
    [PreserveSig]
    int EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);

    [PreserveSig]
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

    [PreserveSig]
    int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

    [PreserveSig]
    int RegisterEndpointNotificationCallback(IntPtr pClient);

    [PreserveSig]
    int UnregisterEndpointNotificationCallback(IntPtr pClient);
}

[ComImport]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceCollection
{
    [PreserveSig]
    int GetCount(out uint pcDevices);

    [PreserveSig]
    int Item(uint nDevice, out IMMDevice ppDevice);
}

[ComImport]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice
{
    [PreserveSig]
    int Activate(in Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.Interface)] out object ppInterface);

    [PreserveSig]
    int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);

    [PreserveSig]
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

    [PreserveSig]
    int GetState(out int pdwState);
}

[ComImport]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPropertyStore
{
    [PreserveSig]
    int GetCount(out uint cProps);

    [PreserveSig]
    int GetAt(uint iProp, out PROPERTYKEY pkey);

    [PreserveSig]
    int GetValue(in PROPERTYKEY key, out PROPVARIANT pv);

    [PreserveSig]
    int SetValue(in PROPERTYKEY key, in PROPVARIANT propvar);

    [PreserveSig]
    int Commit();
}

[ComImport]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioEndpointVolume
{
    [PreserveSig]
    int RegisterControlChangeNotify(IntPtr pNotify);

    [PreserveSig]
    int UnregisterControlChangeNotify(IntPtr pNotify);

    [PreserveSig]
    int GetChannelCount(out uint pnChannelCount);

    [PreserveSig]
    int SetMasterVolumeLevel(float fLevelDB, in Guid pguidEventContext);

    [PreserveSig]
    int SetMasterVolumeLevelScalar(float fLevel, in Guid pguidEventContext);

    [PreserveSig]
    int GetMasterVolumeLevel(out float pfLevelDB);

    [PreserveSig]
    int GetMasterVolumeLevelScalar(out float pfLevel);

    [PreserveSig]
    int SetChannelVolumeLevel(uint nChannel, float fLevelDB, in Guid pguidEventContext);

    [PreserveSig]
    int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, in Guid pguidEventContext);

    [PreserveSig]
    int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

    [PreserveSig]
    int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

    [PreserveSig]
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, in Guid pguidEventContext);

    [PreserveSig]
    int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);

    [PreserveSig]
    int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

    [PreserveSig]
    int VolumeStepUp(in Guid pguidEventContext);

    [PreserveSig]
    int VolumeStepDown(in Guid pguidEventContext);

    [PreserveSig]
    int QueryHardwareSupport(out uint pdwHardwareSupportMask);

    [PreserveSig]
    int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
}

[ComImport]
[Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IAudioClient
{
    [PreserveSig]
    int Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, in Guid audioSessionGuid);

    [PreserveSig]
    int GetBufferSize(out uint pNumBufferFrames);

    [PreserveSig]
    int GetStreamLatency(out long phnsLatency);

    [PreserveSig]
    int GetCurrentPadding(out uint pNumPaddingFrames);

    [PreserveSig]
    int IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);

    [PreserveSig]
    int GetMixFormat(out IntPtr ppDeviceFormat);

    [PreserveSig]
    int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);

    [PreserveSig]
    int Start();

    [PreserveSig]
    int Stop();

    [PreserveSig]
    int Reset();

    [PreserveSig]
    int SetEventHandle(IntPtr eventHandle);

    [PreserveSig]
    int GetService(in Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
}
