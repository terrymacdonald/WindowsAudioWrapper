using Xunit;

namespace WindowsAudioWrapper.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AudioHardwareCollection
{
    public const string Name = "Windows audio hardware integration tests";
}
