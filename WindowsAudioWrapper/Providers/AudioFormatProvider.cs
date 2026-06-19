namespace WindowsAudioWrapper.Providers;

using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Internal.PolicyConfig;
using WindowsAudioWrapper.Models;

internal sealed class AudioFormatProvider : IAudioFormatProvider
{
    private static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00AA00389B71");
    private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00AA00389B71");

    public AudioFormatProfile GetFormat(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));

        IAudioClient audioClient = CoreAudioUtilities.ActivateAudioClient(deviceId);
        IntPtr formatPointer = IntPtr.Zero;

        try
        {
            int hr = audioClient.GetMixFormat(out formatPointer);
            if (hr < 0 || formatPointer == IntPtr.Zero) return new AudioFormatProfile();

            WAVEFORMATEX waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(formatPointer);
            AudioSampleFormat sampleFormat = waveFormat.wFormatTag switch
            {
                1 => AudioSampleFormat.Pcm,
                3 => AudioSampleFormat.IeeeFloat,
                0xFFFE => GetExtensibleSampleFormat(formatPointer),
                _ => AudioSampleFormat.Unknown
            };

            int bitsPerSample = waveFormat.wBitsPerSample;
            if (waveFormat.wFormatTag == 0xFFFE)
            {
                WAVEFORMATEXTENSIBLE extensible = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(formatPointer);
                bitsPerSample = extensible.wValidBitsPerSample > 0 ? extensible.wValidBitsPerSample : waveFormat.wBitsPerSample;
            }

            return new AudioFormatProfile
            {
                Channels = waveFormat.nChannels,
                SampleRate = checked((int)waveFormat.nSamplesPerSec),
                BitsPerSample = bitsPerSample,
                SampleFormat = sampleFormat,
                Mode = AudioFormatMode.Shared
            };
        }
        finally
        {
            if (formatPointer != IntPtr.Zero) Marshal.FreeCoTaskMem(formatPointer);
        }
    }

    public void SetFormat(AudioEndpointReference endpoint, AudioFormatProfile format)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(format);

        if (string.IsNullOrWhiteSpace(endpoint.DeviceId))
            throw new ArgumentException("DeviceId is required.", nameof(endpoint));

        Type? policyConfigType = Type.GetTypeFromCLSID(PolicyConfigInterop.CLSID_PolicyConfigClient);
        if (policyConfigType is null) throw new COMException("Unable to resolve PolicyConfigClient.");

        object? policyConfigObject = Activator.CreateInstance(policyConfigType);
        if (policyConfigObject is not PolicyConfigInterop.IPolicyConfig policyConfig)
            throw new COMException("Unable to create PolicyConfigClient object.");

        WAVEFORMATEXTENSIBLE nativeFormat = CreateWaveFormatExtensible(format);
        IntPtr formatPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf<WAVEFORMATEXTENSIBLE>());

        try
        {
            Marshal.StructureToPtr(nativeFormat, formatPtr, false);
            int hr = policyConfig.SetDeviceFormat(endpoint.DeviceId, formatPtr, formatPtr);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(formatPtr);
        }
    }

    private static AudioSampleFormat GetExtensibleSampleFormat(IntPtr formatPointer)
    {
        WAVEFORMATEXTENSIBLE extensible = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(formatPointer);
        if (extensible.SubFormat == PcmSubFormat) return AudioSampleFormat.Pcm;
        if (extensible.SubFormat == IeeeFloatSubFormat) return AudioSampleFormat.IeeeFloat;
        return AudioSampleFormat.Unknown;
    }

    private static WAVEFORMATEXTENSIBLE CreateWaveFormatExtensible(AudioFormatProfile profile)
    {
        ushort blockAlign = (ushort)(profile.Channels * (profile.BitsPerSample / 8));
        uint avgBytesPerSec = (uint)(profile.SampleRate * blockAlign);

        return new WAVEFORMATEXTENSIBLE
        {
            Format = new WAVEFORMATEX
            {
                wFormatTag = 0xFFFE, // WAVE_FORMAT_EXTENSIBLE
                nChannels = (ushort)profile.Channels,
                nSamplesPerSec = (uint)profile.SampleRate,
                nAvgBytesPerSec = avgBytesPerSec,
                nBlockAlign = blockAlign,
                wBitsPerSample = (ushort)profile.BitsPerSample,
                cbSize = 22 // Size of the extension
            },
            wValidBitsPerSample = (ushort)profile.BitsPerSample,
            dwChannelMask = GetDefaultChannelMask(profile.Channels),
            SubFormat = profile.SampleFormat == AudioSampleFormat.IeeeFloat ? IeeeFloatSubFormat : PcmSubFormat
        };
    }

    private static uint GetDefaultChannelMask(int channels)
    {
        return channels switch
        {
            1 => 0x4, // SPEAKER_FRONT_CENTER
            2 => 0x3, // SPEAKER_FRONT_LEFT | SPEAKER_FRONT_RIGHT
            4 => 0x33, // Quad
            6 => 0x3F, // 5.1
            8 => 0x63F, // 7.1
            _ => 0
        };
    }
}