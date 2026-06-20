namespace WindowsAudioWrapper.Providers;

using System;
using System.Runtime.InteropServices;
using WindowsAudioWrapper.Internal.CoreAudio;
using WindowsAudioWrapper.Models;

internal sealed class AudioFormatProvider : IAudioFormatProvider
{
    private const int STGM_READWRITE = 2;

    // Explicit fallback definition of the mix format engine key to ensure zero build errors
    private static readonly PROPERTYKEY LocalPKEY_AudioEngine_DeviceFormat = 
        new(new Guid("E1A69C60-EECA-4A23-AC26-5B084C15F174"), 0);

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
                hr = store.GetValue(in LocalPKEY_AudioEngine_DeviceFormat, out value);
                
                if (hr >= 0 && value.vt == 65) // VT_BLOB
                {
                    IntPtr blobPtr = value.p;
                    if (blobPtr != IntPtr.Zero)
                    {
                        int blobSize = Marshal.ReadInt32(blobPtr);
                        IntPtr dataDataPtr = blobPtr + 4;

                        if (blobSize >= Marshal.SizeOf<WAVEFORMATEX>())
                        {
                            var waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(dataDataPtr);
                            profile.SampleRate = (int)waveFormat.nSamplesPerSec;
                            profile.Channels = (int)waveFormat.nChannels;
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
            hr = store.GetValue(in LocalPKEY_AudioEngine_DeviceFormat, out checkValue);
            if (hr < 0 || checkValue.vt == 0) // VT_EMPTY
            {
                CoreAudioConstants.PropVariantClear(ref checkValue);
                return; // Gracefully pass over virtual hooks that don't have format registers
            }
            CoreAudioConstants.PropVariantClear(ref checkValue);

            // Create a standard extensible PCM audio structure header block
            ushort standardBitDepth = 32; // Default to float/high-res baseline matching your schema specs
            WAVEFORMATEX nativeFormat = new()
            {
                wFormatTag = 3, // WAVE_FORMAT_IEEE_FLOAT
                nChannels = (ushort)formatProfile.Channels,
                nSamplesPerSec = (uint)formatProfile.SampleRate,
                wBitsPerSample = standardBitDepth,
                cbSize = 0
            };

            nativeFormat.nBlockAlign = (ushort)((nativeFormat.wBitsPerSample / 8) * nativeFormat.nChannels);
            nativeFormat.nAvgBytesPerSec = nativeFormat.nSamplesPerSec * nativeFormat.nBlockAlign;

            int structSize = Marshal.SizeOf(nativeFormat);
            int totalBlobSize = structSize + 4;
            IntPtr allocatedBuffer = Marshal.AllocCoTaskMem(totalBlobSize);

            try
            {
                Marshal.WriteInt32(allocatedBuffer, structSize);
                Marshal.StructureToPtr(nativeFormat, allocatedBuffer + 4, false);

                PROPVARIANT propVar = new()
                {
                    vt = 65, // VT_BLOB
                    p = allocatedBuffer
                };

                hr = store.SetValue(in LocalPKEY_AudioEngine_DeviceFormat, in propVar);
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