namespace WindowsAudioWrapper.Providers;

internal interface ISystemAudioProvider
{
    bool IsMonoAudioReadSupported { get; }

    bool IsMonoAudioSetSupported { get; }

    bool GetMonoAudio();

    void SetMonoAudio(bool enabled);
}
