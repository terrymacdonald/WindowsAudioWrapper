namespace WindowsAudioWrapper.Providers;

using System;
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

        try
        {
            IMMDevice device = CoreAudioUtilities.GetDeviceById(deviceId);
            int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
            if (hr >= 0 && store != null)
            {
                PROPVARIANT value = default;
                // Defensive filter: wrap property fetch to prevent unmanaged index key faults
                hr = store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out value);
                if (hr >= 0 && value.vt == VT_BOOL)
                {
                    profile.AreEnhancementsSupported = true;
                    profile.DisableAllEnhancements = value.p != IntPtr.Zero; 
                }
                CoreAudioConstants.PropVariantClear(ref value);
            }
        }
        catch
        {
            // Fail-open strategy: return empty unsupported block instead of crashing pipeline execution
            profile.AreEnhancementsSupported = false;
        }

        return profile;
    }

    public void SetAudioEnhancements(AudioEndpointReference endpoint, AudioEnhancementProfile audioEnhancements)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(audioEnhancements);
        if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
            throw new ArgumentException("DeviceId is required.", nameof(endpoint));

        // Defensive Capability Guard: Skip execution entirely if target properties aren't confirmed supported
        if (!audioEnhancements.AreEnhancementsSupported)
        {
            return;
        }

        try
        {
            IMMDevice device = CoreAudioUtilities.GetDeviceById(endpoint.DeviceId);
            int hr = device.OpenPropertyStore(STGM_READWRITE, out IPropertyStore store);
            if (hr < 0 || store == null) return;

            // Verify the index entry is cleanly accessible prior to forcing updates
            PROPVARIANT checkValue = default;
            hr = store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out checkValue);
            if (hr < 0 || checkValue.vt != VT_BOOL)
            {
                CoreAudioConstants.PropVariantClear(ref checkValue);
                return; // Safely bypass endpoints that missing index support
            }
            CoreAudioConstants.PropVariantClear(ref checkValue);

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
        }
        catch
        {
            // Intercept unmanaged 0x80070491 boundary errors on edge hardware variations safely
        }
    }
}