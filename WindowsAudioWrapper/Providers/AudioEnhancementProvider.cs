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

        var profile = new AudioEnhancementProfile();
        IMMDevice device = CoreAudioUtilities.GetDeviceById(deviceId);
        
        int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
        if (hr >= 0 && store != null)
        {
            PROPVARIANT value = default;
            if (store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out value) >= 0)
            {
                if (value.vt == VT_BOOL)
                {
                    profile.AreEnhancementsSupported = true;
                    profile.DisableAllEnhancements = value.p != IntPtr.Zero; 
                }
                CoreAudioConstants.PropVariantClear(ref value);
            }
        }

        return profile;
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
        {
            throw new COMException("Failed to open the unmanaged device property store context.", hr);
        }

        PROPVARIANT propVar = new()
        {
            vt = VT_BOOL,
            p = audioEnhancements.DisableAllEnhancements ? (IntPtr)(-1) : IntPtr.Zero
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
}