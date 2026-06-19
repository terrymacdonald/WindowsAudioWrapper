namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the default format of the audio stream for a given endpoint.
/// </summary>
public sealed class AudioFormatProfile
{
    /// <summary>Gets or sets the number of audio channels (e.g., 2 for stereo).</summary>
    public int Channels { get; set; } = 2;

    /// <summary>Gets or sets the sample rate in Hertz (e.g., 48000).</summary>
    public int SampleRate { get; set; } = 48000;

    /// <summary>Gets or sets the bit depth per sample (e.g., 16, 24, 32).</summary>
    public int BitsPerSample { get; set; } = 24;

    /// <summary>Gets or sets the underlying data format (e.g., PCM, IEEE Float).</summary>
    public AudioSampleFormat SampleFormat { get; set; } = AudioSampleFormat.Pcm;

    /// <summary>Gets or sets the mode of the format (Shared or Exclusive).</summary>
    public AudioFormatMode Mode { get; set; } = AudioFormatMode.Shared;
}