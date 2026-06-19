namespace WindowsAudioWrapper.Providers;

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;

internal sealed class AudioEnhancementProvider : IAudioEnhancementProvider
{
    private const int STGM_READWRITE = 2;
    private const ushort VT_BOOL = 11;

    public AudioEnhancementProfile GetAudioEnhancements(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        bool disableSysFx = false;
        IMMDevice device = CoreAudioUtilities.GetDeviceById(deviceId);
        
        int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IntPtr storePtr);
        if (hr >= 0 && storePtr != IntPtr.Zero)
        {
            try
            {
                var strategy = new StrategyBasedComWrappers();
                var store = (IPropertyStore)strategy.GetOrCreateObjectForComInstance(storePtr, CreateObjectFlags.None);
                
                PROPVARIANT value = default;
                if (store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out value) >= 0)
                {
                    if (value.vt == VT_BOOL)
                    {
                        // VARIANT_TRUE is -1, VARIANT_FALSE is 0
                        disableSysFx = value.p != IntPtr.Zero;
                    }
                    CoreAudioConstants.PropVariantClear(ref value);
                }
            }
            finally
            {
                Marshal.Release(storePtr);
            }
        }

        return new AudioEnhancementProfile
        {
            // If Disable_SysFx is false, enhancements are ENABLED.
            IsDeviceDefaultEffectsEnabled = !disableSysFx 
        };
    }

    public void SetAudioEnhancements(AudioEndpointReference endpoint, AudioEnhancementProfile audioEnhancements)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(audioEnhancements);

        if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
            throw new ArgumentException("DeviceId is required.", nameof(endpoint));

        IMMDevice device = CoreAudioUtilities.GetDeviceById(endpoint.DeviceId);
        
        int hr = device.OpenPropertyStore(STGM_READWRITE, out IntPtr storePtr);
        if (hr < 0 || storePtr == IntPtr.Zero)
            Marshal.ThrowExceptionForHR(hr);

        try
        {
            var strategy = new StrategyBasedComWrappers();
            var store = (IPropertyStore)strategy.GetOrCreateObjectForComInstance(storePtr, CreateObjectFlags.None);

            PROPVARIANT propVar = new PROPVARIANT
            {
                vt = VT_BOOL,
                // Invert the logic: if we want effects ENABLED, we set Disable_SysFx to FALSE (0).
                p = (IntPtr)(audioEnhancements.IsDeviceDefaultEffectsEnabled ? 0 : -1)
            };

            hr = store.SetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, in propVar);
            if (hr >= 0)
            {
                store.Commit();
            }
            else
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        finally
        {
            Marshal.Release(storePtr);
        }
    }
}