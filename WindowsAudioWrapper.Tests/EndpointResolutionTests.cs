namespace WindowsAudioWrapper.Tests;

using WindowsAudioWrapper.Models;
using WindowsAudioWrapper.Providers;

public sealed class EndpointResolutionTests
{
    [Fact]
    public void ExactIdTakesPriorityOverAmbiguousContainerAndName()
    {
        AudioEndpointInfo first = EndpointTestProviders.Device("one");
        AudioEndpointInfo second = EndpointTestProviders.Device("two");
        Assert.Same(second, AudioEndpointResolver.Resolve(new() { DeviceId = "TWO", ContainerId = "container", FriendlyName = "Speaker" }, AudioFlow.Render, new[] { first, second }));
    }

    [Fact]
    public void OppositeDirectionCannotSatisfyAContainerMatch()
    {
        AudioEndpointInfo microphone = EndpointTestProviders.Device("microphone", AudioFlow.Capture);
        AudioEndpointInfo result = AudioEndpointResolver.Resolve(new() { DeviceId = "speaker", ContainerId = "container", Flow = AudioFlow.Render }, AudioFlow.Render, new[] { microphone });
        Assert.True(result.State.HasFlag(AudioDeviceState.NotPresent));
    }

    [Theory]
    [InlineData(AudioFlow.Render)]
    [InlineData(AudioFlow.Unknown)]
    public void DuplicateFallbackIsRejected(AudioFlow flow)
    {
        Assert.Throws<AmbiguousAudioEndpointException>(() => AudioEndpointResolver.Resolve(new() { ContainerId = "container" }, flow,
            new[] { EndpointTestProviders.Device("one"), EndpointTestProviders.Device("two") }));
    }

    [Theory]
    [InlineData("container")]
    [InlineData("full-name")]
    [InlineData("friendly-name")]
    public void UniqueFallbackResolvesWithoutChangingSavedReference(string tier)
    {
        AudioEndpointInfo device = EndpointTestProviders.Device("new-id");
        AudioEndpointReference saved = new() { DeviceId = "old-id" };
        if (tier == "container") saved.ContainerId = "container";
        if (tier == "full-name") saved.FullName = "Speaker full";
        if (tier == "friendly-name") saved.FriendlyName = "Speaker";
        Assert.Same(device, AudioEndpointResolver.Resolve(saved, AudioFlow.Unknown, new[] { device }));
        Assert.Equal("old-id", saved.DeviceId);
    }

    [Fact]
    public void DisabledExactMatchDoesNotFallThroughToAnotherActiveDevice()
    {
        AudioEndpointInfo exact = EndpointTestProviders.Device("exact"); exact.State = AudioDeviceState.Disabled;
        Assert.Same(exact, AudioEndpointResolver.Resolve(new() { DeviceId = "exact", ContainerId = "container" }, AudioFlow.Render,
            new[] { EndpointTestProviders.Device("other"), exact }));
    }

    [Fact]
    public void UnknownDirectionRequiresAUniqueEndpointAcrossBothDirections()
    {
        Assert.Throws<AmbiguousAudioEndpointException>(() => AudioEndpointResolver.Resolve(new() { ContainerId = "container" }, AudioFlow.Unknown,
            new[] { EndpointTestProviders.Device("speaker"), EndpointTestProviders.Device("microphone", AudioFlow.Capture) }));
    }

    [Fact]
    public void ExplicitWrongDirectionDoesNotResolveEvenWithAnExactId()
    {
        AudioEndpointInfo result = AudioEndpointResolver.Resolve(new() { DeviceId = "speaker", Flow = AudioFlow.Capture }, AudioFlow.Render,
            new[] { EndpointTestProviders.Device("speaker") });
        Assert.True(result.State.HasFlag(AudioDeviceState.NotPresent));
    }
}
