using System.Text.Json.Serialization;

namespace WindowsAudioWrapper.Models;

/// <summary>
/// Represents the default format of the audio stream for a given endpoint.
/// Adjusted to cleanly match schema specifications without losing internal enumeration settings.
/// </summary>
public sealed class AudioFormatProfile
{
    /// <summary>Gets or sets the sample rate in Hertz (e.g., 48000, 192000).</summary>
    public int SampleRate { get; set; } = 48000;

    /// <summary>Gets or sets the bit depth precision mapping allocation metrics per sample (e.g., 16, 24, 32).</summary>
    [JsonPropertyName("BitDepth")]
    public int BitsPerSample { get; set; } = 24;

    /// <summary>Gets or sets the number of audio channels (e.g., 2 for stereo).</summary>
    public int Channels { get; set; } = 2;

    /// <summary>Gets or sets the underlying data format layout tag descriptor. Ignored in JSON.</summary>
    [JsonIgnore]
    public AudioSampleFormat SampleFormat { get; set; } = AudioSampleFormat.Pcm;

    /// <summary>Gets or sets the resource access concurrency mode of the format context. Ignored in JSON.</summary>
    [JsonIgnore]
    public AudioFormatMode Mode { get; set; } = AudioFormatMode.Shared;
}