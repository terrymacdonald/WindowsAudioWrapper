namespace WindowsAudioWrapper.Internal.CoreAudio;

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

internal static class CoreAudioInterop
{
    internal const int CLSCTX_INPROC_SERVER = 0x1;
    internal const int CLSCTX_ALL = 0x17;
    internal const int STGM_READ = 0;
    internal const int DEVICE_STATE_ACTIVE = 0x00000001;
    internal const int DEVICE_STATE_DISABLED = 0x00000002;
    internal const int DEVICE_STATE_NOTPRESENT = 0x00000004;
    internal const int DEVICE_STATE_UNPLUGGED = 0x00000008;
    internal const int AUDCLNT_SHAREMODE_SHARED = 0;
    internal const int AUDCLNT_SHAREMODE_EXCLUSIVE = 1;

    internal static readonly Guid CLSID_MMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    internal static readonly Guid IID_IAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    internal static readonly Guid IID_IAudioClient = new("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal sealed class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2
    }

    internal enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);

        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

        void RegisterEndpointNotificationCallback(IntPtr pClient);

        void UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-C0A9F7F2A0B8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceCollection
    {
        void GetCount(out uint pcDevices);

        void Item(uint nDevice, out IMMDevice ppDevice);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        void Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        void OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);

        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        void GetState(out int pdwState);
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        void GetCount(out uint cProps);

        void GetAt(uint iProp, out PROPERTYKEY pkey);

        void GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);

        void SetValue(ref PROPERTYKEY key, ref PROPVARIANT propvar);

        void Commit();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;

        public PROPERTYKEY(Guid fmtid, uint pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPVARIANT
    {
        public ushort vt;
        private ushort wReserved1;
        private ushort wReserved2;
        private ushort wReserved3;
        public IntPtr p;
        private int p2;

        public string GetString()
        {
            return vt == 31 && p != IntPtr.Zero ? Marshal.PtrToStringUni(p) ?? string.Empty : string.Empty;
        }

        public Guid GetGuid()
        {
            if (vt == 72 && p != IntPtr.Zero)
            {
                return Marshal.PtrToStructure<Guid>(p);
            }

            return Guid.Empty;
        }
    }

    internal static readonly PROPERTYKEY PKEY_Device_FriendlyName = new(
        new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);

    internal static readonly PROPERTYKEY PKEY_DeviceInterface_FriendlyName = new(
        new Guid("026E516E-B814-414B-83CD-856D6FEF4822"), 2);

    internal static readonly PROPERTYKEY PKEY_Device_ContainerId = new(
        new Guid("8C7ED206-3F8A-4827-B3AB-AE9E1FAEFC6C"), 2);

    [DllImport("ole32.dll")]
    internal static extern int PropVariantClear(ref PROPVARIANT pvar);

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify(IntPtr pNotify);

        void UnregisterControlChangeNotify(IntPtr pNotify);

        void GetChannelCount(out uint pnChannelCount);

        void SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);

        void SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);

        void GetMasterVolumeLevel(out float pfLevelDB);

        void GetMasterVolumeLevelScalar(out float pfLevel);

        void SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);

        void SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);

        void GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);

        void GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);

        void SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);

        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);

        void GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);

        void VolumeStepUp(ref Guid pguidEventContext);

        void VolumeStepDown(ref Guid pguidEventContext);

        void QueryHardwareSupport(out uint pdwHardwareSupportMask);

        void GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    [ComImport]
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClient
    {
        void Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, ref Guid audioSessionGuid);

        void GetBufferSize(out uint pNumBufferFrames);

        void GetStreamLatency(out long phnsLatency);

        void GetCurrentPadding(out uint pNumPaddingFrames);

        void IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);

        void GetMixFormat(out IntPtr ppDeviceFormat);

        void GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);

        void Start();

        void Stop();

        void Reset();

        void SetEventHandle(IntPtr eventHandle);

        void GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct WAVEFORMATEX
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
    internal struct WAVEFORMATEXTENSIBLE
    {
        public WAVEFORMATEX Format;
        public ushort wValidBitsPerSample;
        public uint dwChannelMask;
        public Guid SubFormat;
    }
}
