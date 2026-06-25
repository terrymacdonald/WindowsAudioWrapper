namespace WindowsAudioWrapper.Providers;

using System;
using System.Collections.Generic;
using System.Linq;
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

        PopulateActiveEffects(deviceId, profile);
        PopulateDisableAllEnhancements(deviceId, profile);

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

        TrySetDisableAllEnhancements(endpoint.DeviceId, audioEnhancements.DisableAllEnhancements);

        if (audioEnhancements.DisableAllEnhancements)
        {
            ApplyActiveEffectStates(endpoint.DeviceId, Array.Empty<string>());
        }
        else
        {
            ApplyActiveEffectStates(endpoint.DeviceId, audioEnhancements.ActiveEffectsGuidsList);
        }
    }

    private static void PopulateDisableAllEnhancements(string deviceId, AudioEnhancementProfile profile)
    {
        try
        {
            IMMDevice device = CoreAudioUtilities.GetDeviceById(deviceId);
            int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
            if (hr < 0 || store == null)
            {
                return;
            }

            PROPVARIANT value = default;
            try
            {
                // Defensive filter: wrap property fetch to prevent unmanaged index key faults
                hr = store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out value);
                if (hr >= 0 && value.vt == VT_BOOL)
                {
                    profile.AreEnhancementsSupported = true;
                    profile.DisableAllEnhancements = value.p != IntPtr.Zero;
                }
            }
            finally
            {
                CoreAudioConstants.PropVariantClear(ref value);
            }
        }
        catch
        {
            // Fail-open strategy: keep any effect-manager data already captured.
        }
    }

    private static void PopulateActiveEffects(string deviceId, AudioEnhancementProfile profile)
    {
        IReadOnlyList<AUDIO_EFFECT> effects = CoreAudioUtilities.GetAudioEffects(deviceId);
        if (effects.Count == 0)
        {
            return;
        }

        profile.AreEnhancementsSupported = true;
        profile.ActiveEffectsGuidsList = effects
            .Where(effect => effect.id != Guid.Empty && effect.state == AudioEffectState.On)
            .Select(effect => effect.id.ToString("D"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(effectId => effectId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void ApplyActiveEffectStates(string deviceId, IReadOnlyCollection<string>? desiredActiveEffectIds)
    {
        HashSet<Guid> desiredActiveEffects = BuildDesiredActiveEffects(desiredActiveEffectIds);
        if (!CoreAudioUtilities.TryActivateAudioEffectsManager(deviceId, out IAudioClient? audioClient, out IAudioEffectsManager? audioEffectsManager) ||
            audioClient == null ||
            audioEffectsManager == null)
        {
            return;
        }

        IntPtr effectsPointer = IntPtr.Zero;
        try
        {
            int hr = audioEffectsManager.GetAudioEffects(out effectsPointer, out uint effectCount);
            if (hr < 0 || effectsPointer == IntPtr.Zero || effectCount == 0)
            {
                return;
            }

            int effectSize = Marshal.SizeOf<AUDIO_EFFECT>();
            for (uint index = 0; index < effectCount; index++)
            {
                IntPtr effectPointer = IntPtr.Add(effectsPointer, checked((int)(index * (uint)effectSize)));
                AUDIO_EFFECT effect = Marshal.PtrToStructure<AUDIO_EFFECT>(effectPointer);
                if (effect.id == Guid.Empty || !effect.canSetState)
                {
                    continue;
                }

                AudioEffectState desiredState = desiredActiveEffects.Contains(effect.id) ? AudioEffectState.On : AudioEffectState.Off;
                if (effect.state == desiredState)
                {
                    continue;
                }

                hr = audioEffectsManager.SetAudioEffectState(effect.id, desiredState);
                if (hr == CoreAudioConstants.AUDCLNT_E_EFFECT_NOT_AVAILABLE ||
                    hr == CoreAudioConstants.AUDCLNT_E_EFFECT_STATE_READ_ONLY)
                {
                    continue;
                }
            }

            GC.KeepAlive(audioClient);
        }
        catch
        {
            // Effect lists and mutability are hardware/driver dependent; profile application must remain stable.
        }
        finally
        {
            if (effectsPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(effectsPointer);
            }
        }
    }

    private static void TrySetDisableAllEnhancements(string deviceId, bool disableAllEnhancements)
    {
        try
        {
            IMMDevice device = CoreAudioUtilities.GetDeviceById(deviceId);
            int hr = device.OpenPropertyStore(STGM_READWRITE, out IPropertyStore store);
            if (hr < 0 || store == null)
            {
                return;
            }

            // Verify the index entry is cleanly accessible prior to forcing updates
            PROPVARIANT checkValue = default;
            try
            {
                hr = store.GetValue(in CoreAudioConstants.PKEY_AudioEndpoint_Disable_SysFx, out checkValue);
                if (hr < 0 || checkValue.vt != VT_BOOL)
                {
                    return; // Safely bypass endpoints that missing index support
                }
            }
            finally
            {
                CoreAudioConstants.PropVariantClear(ref checkValue);
            }

            PROPVARIANT propVar = new()
            {
                vt = VT_BOOL,
                p = disableAllEnhancements ? (IntPtr)(-1) : IntPtr.Zero
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

    private static HashSet<Guid> BuildDesiredActiveEffects(IReadOnlyCollection<string>? desiredActiveEffectIds)
    {
        var desiredActiveEffects = new HashSet<Guid>();
        if (desiredActiveEffectIds == null)
        {
            return desiredActiveEffects;
        }

        foreach (string effectId in desiredActiveEffectIds)
        {
            if (Guid.TryParse(effectId, out Guid parsedEffectId) && parsedEffectId != Guid.Empty)
            {
                desiredActiveEffects.Add(parsedEffectId);
            }
        }

        return desiredActiveEffects;
    }
}
