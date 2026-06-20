namespace WindowsAudioWrapper.Providers;

using System.Runtime.InteropServices;
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
        
        int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
        if (hr >= 0 && store != null)
        {
            PROPVARIANT value = default;
            if (store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out value) >= 0)
            {
                if (value.vt == VT_BOOL)
                {
                    disableSysFx = value.p != IntPtr.Zero;
                }
                CoreAudioConstants.PropVariantClear(ref value);
            }
        }

        return new AudioEnhancementProfile
        {
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
        
        int hr = device.OpenPropertyStore(STGM_READWRITE, out IPropertyStore store);
        if (hr < 0 || store == null)
            Marshal.ThrowExceptionForHR(hr);

        PROPVARIANT propVar = new PROPVARIANT
        {
            vt = VT_BOOL,
            p = audioEnhancements.IsDeviceDefaultEffectsEnabled ? IntPtr.Zero : (IntPtr)(-1)
        };

        if (store != null)
        {
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
    }
}