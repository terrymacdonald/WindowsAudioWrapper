namespace WindowsAudioWrapper.Providers;

using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;

internal sealed class AudioFormatProvider : IAudioFormatProvider
{
    private static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00AA00389B71");
    private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00AA00389B71");

    public AudioFormatProfile GetFormat(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        IAudioClient audioClient = CoreAudioUtilities.ActivateAudioClient(deviceId);
        IntPtr formatPointer = IntPtr.Zero;

        try
        {
            int hr = audioClient.GetMixFormat(out formatPointer);
            if (hr < 0 || formatPointer == IntPtr.Zero)
            {
                return new AudioFormatProfile(); // Default to fallback properties if unreadable
            }

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
            if (formatPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(formatPointer);
            }
        }
    }

    public void SetFormat(AudioEndpointReference endpoint, AudioFormatProfile format)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(format);
        throw new NotSupportedException("Setting the Windows endpoint default audio format is not supported by this version of WindowsAudioWrapper.");
    }

    private static AudioSampleFormat GetExtensibleSampleFormat(IntPtr formatPointer)
    {
        WAVEFORMATEXTENSIBLE extensible = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(formatPointer);

        if (extensible.SubFormat == PcmSubFormat)
        {
            return AudioSampleFormat.Pcm;
        }

        if (extensible.SubFormat == IeeeFloatSubFormat)
        {
            return AudioSampleFormat.IeeeFloat;
        }

        return AudioSampleFormat.Unknown;
    }
}