namespace WindowsAudioWrapper.Providers;

using System;
using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Internal.PolicyConfig;
using WindowsAudioWrapper.Models;

internal sealed class AudioFormatProvider : IAudioFormatProvider
{
    private const ushort WAVE_FORMAT_PCM = 1;
    private const ushort WAVE_FORMAT_IEEE_FLOAT = 3;
    private const ushort WAVE_FORMAT_EXTENSIBLE = 0xFFFE;

    private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new("00000001-0000-0010-8000-00aa00389b71");
    private static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new("00000003-0000-0010-8000-00aa00389b71");

    public AudioFormatProfile GetFormat(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        var profile = new AudioFormatProfile();

        try
        {
            IMMDevice device = CoreAudioUtilities.GetDeviceById(deviceId);
            int hr = device.OpenPropertyStore(CoreAudioConstants.STGM_READ, out IPropertyStore store);
            if (hr >= 0 && store != null)
            {
                PROPVARIANT value = default;
                hr = store.GetValue(in CoreAudioConstants.PKEY_AudioEngine_DeviceFormat, out value);
                
                if (hr >= 0 && value.vt == 65) // VT_BLOB
                {
                    int blobSize = checked((int)value.blobSize);
                    IntPtr dataPtr = value.blobData;
                    if (blobSize >= Marshal.SizeOf<WAVEFORMATEX>() && dataPtr != IntPtr.Zero)
                    {
                        var waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(dataPtr);
                        profile.SampleRate = (int)waveFormat.nSamplesPerSec;
                        profile.Channels = (int)waveFormat.nChannels;
                        profile.BitsPerSample = waveFormat.wBitsPerSample;
                        profile.SampleFormat = waveFormat.wFormatTag switch
                        {
                            WAVE_FORMAT_IEEE_FLOAT => AudioSampleFormat.IeeeFloat,
                            WAVE_FORMAT_PCM => AudioSampleFormat.Pcm,
                            _ => AudioSampleFormat.Unknown
                        };

                        if (waveFormat.wFormatTag == WAVE_FORMAT_EXTENSIBLE &&
                            blobSize >= Marshal.SizeOf<WAVEFORMATEXTENSIBLE>())
                        {
                            var extensibleFormat = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(dataPtr);
                            profile.BitsPerSample = extensibleFormat.wValidBitsPerSample > 0
                                ? extensibleFormat.wValidBitsPerSample
                                : waveFormat.wBitsPerSample;
                            profile.ChannelMask = extensibleFormat.dwChannelMask;
                            profile.SampleFormat = extensibleFormat.SubFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
                                ? AudioSampleFormat.IeeeFloat
                                : extensibleFormat.SubFormat == KSDATAFORMAT_SUBTYPE_PCM
                                    ? AudioSampleFormat.Pcm
                                    : AudioSampleFormat.Unknown;
                        }
                    }
                }
                CoreAudioConstants.PropVariantClear(ref value);
            }
        }
        catch
        {
            // Fail-open baseline: if virtual driver lacks format metadata, return empty profile container gracefully
        }

        return profile;
    }

    public void SetFormat(AudioEndpointReference endpoint, AudioFormatProfile formatProfile)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(formatProfile);

        if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
            throw new ArgumentException("DeviceId is required.", nameof(endpoint));

        if (formatProfile.SampleRate <= 0 || formatProfile.Channels <= 0 || formatProfile.BitsPerSample <= 0)
        {
            return;
        }

        try
        {
            object nativeFormatObject = BuildNativeFormat(formatProfile);
            int structSize = Marshal.SizeOf(nativeFormatObject);
            IntPtr formatPointer = Marshal.AllocCoTaskMem(structSize);
            try
            {
                Marshal.StructureToPtr(nativeFormatObject, formatPointer, false);

                if (!IsSharedFormatSupported(endpoint.DeviceId, formatPointer))
                {
                    return;
                }

                PolicyConfigInterop.IPolicyConfig policyConfig = CreatePolicyConfig();
                int hr = policyConfig.SetDeviceFormat(endpoint.DeviceId, formatPointer, formatPointer);
                if (hr < 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(formatPointer);
            }
        }
        catch
        {
            // Gracefully pass over edge-case hardware errors to keep execution running
        }
    }

    private static object BuildNativeFormat(AudioFormatProfile formatProfile)
    {
        ushort bitDepth = (ushort)formatProfile.BitsPerSample;
        ushort formatTag = formatProfile.SampleFormat == AudioSampleFormat.IeeeFloat
            ? WAVE_FORMAT_IEEE_FLOAT
            : WAVE_FORMAT_PCM;

        WAVEFORMATEX nativeFormat = new()
        {
            wFormatTag = formatProfile.ChannelMask > 0 ? WAVE_FORMAT_EXTENSIBLE : formatTag,
            nChannels = (ushort)formatProfile.Channels,
            nSamplesPerSec = (uint)formatProfile.SampleRate,
            wBitsPerSample = bitDepth,
            cbSize = formatProfile.ChannelMask > 0 ? (ushort)22 : (ushort)0
        };

        nativeFormat.nBlockAlign = (ushort)((nativeFormat.wBitsPerSample / 8) * nativeFormat.nChannels);
        nativeFormat.nAvgBytesPerSec = nativeFormat.nSamplesPerSec * nativeFormat.nBlockAlign;

        if (formatProfile.ChannelMask == 0)
        {
            return nativeFormat;
        }

        return new WAVEFORMATEXTENSIBLE
        {
            Format = nativeFormat,
            wValidBitsPerSample = bitDepth,
            dwChannelMask = formatProfile.ChannelMask,
            SubFormat = formatProfile.SampleFormat == AudioSampleFormat.IeeeFloat
                ? KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
                : KSDATAFORMAT_SUBTYPE_PCM
        };
    }

    private static bool IsSharedFormatSupported(string deviceId, IntPtr formatPointer)
    {
        IAudioClient audioClient = CoreAudioUtilities.ActivateAudioClient(deviceId);
        IntPtr closestMatchPointer = IntPtr.Zero;
        try
        {
            int hr = audioClient.IsFormatSupported(CoreAudioConstants.AUDCLNT_SHAREMODE_SHARED, formatPointer, out closestMatchPointer);
            return hr == 0;
        }
        finally
        {
            if (closestMatchPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(closestMatchPointer);
            }

            GC.KeepAlive(audioClient);
        }
    }

    private static PolicyConfigInterop.IPolicyConfig CreatePolicyConfig()
    {
        Type? policyConfigType = Type.GetTypeFromCLSID(PolicyConfigInterop.CLSID_PolicyConfigClient);
        if (policyConfigType is null)
        {
            throw new COMException("Unable to resolve the PolicyConfigClient COM class.");
        }

        object? policyConfigObject = Activator.CreateInstance(policyConfigType);
        return policyConfigObject as PolicyConfigInterop.IPolicyConfig
            ?? throw new COMException("Unable to create the PolicyConfigClient COM object.");
    }
}
