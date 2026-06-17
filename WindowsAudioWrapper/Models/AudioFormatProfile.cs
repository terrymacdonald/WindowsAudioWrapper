namespace WindowsAudioWrapper.Models;

public sealed class AudioFormatProfile
{
    public int Channels { get; set; } = 2;

    public int SampleRate { get; set; } = 48000;

    public int BitsPerSample { get; set; } = 24;

    public AudioSampleFormat SampleFormat { get; set; } = AudioSampleFormat.Pcm;

    public AudioFormatMode Mode { get; set; } = AudioFormatMode.Shared;
}
