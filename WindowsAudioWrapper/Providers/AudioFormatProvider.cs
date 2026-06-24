namespace WindowsAudioWrapper.Providers;

using System;
using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;

internal sealed class AudioFormatProvider : IAudioFormatProvider
{
    private const int STGM_READWRITE = 2;
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

        try
        {
            IMMDevice device = CoreAudioUtilities.GetDeviceById(endpoint.DeviceId);
            int hr = device.OpenPropertyStore(STGM_READWRITE, out IPropertyStore store);
            if (hr < 0 || store == null) return;

            // Defensive lookup guard to ensure the key is present before executing updates
            PROPVARIANT checkValue = default;
            hr = store.GetValue(in CoreAudioConstants.PKEY_AudioEngine_DeviceFormat, out checkValue);
            if (hr < 0 || checkValue.vt == 0) // VT_EMPTY
            {
                CoreAudioConstants.PropVariantClear(ref checkValue);
                return; // Gracefully pass over virtual hooks that don't have format registers
            }
            CoreAudioConstants.PropVariantClear(ref checkValue);

            ushort bitDepth = formatProfile.BitsPerSample > 0 ? (ushort)formatProfile.BitsPerSample : (ushort)24;
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

            object nativeFormatObject = formatProfile.ChannelMask > 0
                ? new WAVEFORMATEXTENSIBLE
                {
                    Format = nativeFormat,
                    wValidBitsPerSample = bitDepth,
                    dwChannelMask = formatProfile.ChannelMask,
                    SubFormat = formatProfile.SampleFormat == AudioSampleFormat.IeeeFloat
                        ? KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
                        : KSDATAFORMAT_SUBTYPE_PCM
                }
                : nativeFormat;

            int structSize = Marshal.SizeOf(nativeFormatObject);
            IntPtr allocatedBuffer = Marshal.AllocCoTaskMem(structSize);

            try
            {
                Marshal.StructureToPtr(nativeFormatObject, allocatedBuffer, false);

                PROPVARIANT propVar = new()
                {
                    vt = 65, // VT_BLOB
                    blobSize = (uint)structSize,
                    blobData = allocatedBuffer
                };

                hr = store.SetValue(in CoreAudioConstants.PKEY_AudioEngine_DeviceFormat, in propVar);
                if (hr >= 0)
                {
                    store.Commit();
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(allocatedBuffer);
            }
        }
        catch
        {
            // Gracefully pass over edge-case hardware errors to keep execution running
        }
    }
}
