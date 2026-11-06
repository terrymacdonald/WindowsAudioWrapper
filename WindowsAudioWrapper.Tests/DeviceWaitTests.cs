namespace WindowsAudioWrapper.Tests;

using WindowsAudioWrapper.Models;

public sealed class DeviceWaitTests
{
    [Fact]
    public async Task MicrophoneArrivalDoesNotCompleteASpeakerWait()
    {
        EndpointTestProviders providers = new();
        AudioEndpointInfo[] inventory = Array.Empty<AudioEndpointInfo>();
        providers.ReadInventory = () => Volatile.Read(ref inventory);
        using WindowsAudioController controller = providers.Controller();
        Task<bool> wait = controller.WaitForAudioDeviceToAppearAsync(new() { DeviceId = "speaker", ContainerId = "container", Flow = AudioFlow.Render }, 80);
        Volatile.Write(ref inventory, new[] { EndpointTestProviders.Device("microphone", AudioFlow.Capture) });
        controller.ProcessDeviceNotification("microphone");
        Assert.False(await wait.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task FailedNotificationsStillDiscoverALaterDeviceByBoundedRecheck()
    {
        EndpointTestProviders providers = new();
        AudioEndpointInfo[] inventory = Array.Empty<AudioEndpointInfo>();
        providers.ReadInventory = () => Volatile.Read(ref inventory);
        using WindowsAudioController controller = providers.Controller();
        Task<bool> wait = controller.WaitForAudioDeviceToAppearAsync(new() { DeviceId = "old", ContainerId = "container", Flow = AudioFlow.Render }, 1500);
        Volatile.Write(ref inventory, new[] { EndpointTestProviders.Device("new") });
        Assert.True(await wait.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(providers.RegistrationAttempts > 0);
    }

    [Fact]
    public async Task PreCancelledWaitDoesNotReadDevices()
    {
        EndpointTestProviders providers = new() { ReadInventory = () => throw new InvalidOperationException("Cancelled wait performed a read.") };
        using WindowsAudioController controller = providers.Controller();
        using CancellationTokenSource cancellation = new(); cancellation.Cancel();
        Assert.False(await controller.WaitForAudioDeviceToAppearAsync(new() { DeviceId = "device" }, 1000, cancellation.Token));
    }

    [Fact]
    public async Task CancellationWakesAMissingDeviceWait()
    {
        EndpointTestProviders providers = new();
        using WindowsAudioController controller = providers.Controller();
        using CancellationTokenSource cancellation = new();
        Task<bool> wait = controller.WaitForAudioDeviceToAppearAsync(new() { DeviceId = "missing" }, 60000, cancellation.Token);
        cancellation.Cancel();
        Assert.False(await wait.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task DisposalWakesWaitAndRejectsNewWaits()
    {
        EndpointTestProviders providers = new();
        WindowsAudioController controller = providers.Controller();
        Task<bool> wait = controller.WaitForAudioDeviceToAppearAsync(new() { DeviceId = "missing" }, 60000);
        controller.Dispose();
        Assert.False(await wait.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.True(providers.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => controller.WaitForAudioDeviceToAppearAsync(new(), 100));
        controller.Dispose();
    }

    [Fact]
    public void SynchronousWaitUsesTheSameUniqueFallback()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("new"));
        using WindowsAudioController controller = providers.Controller();
        Assert.True(controller.WaitForAudioDeviceToAppear(new() { DeviceId = "old", ContainerId = "container", Flow = AudioFlow.Render }, 0));
    }

    [Fact]
    public async Task RepeatedUnrelatedEventsDoNotRestartTheTimeout()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("unrelated", container: "other"));
        using WindowsAudioController controller = providers.Controller();
        Task<bool> wait = controller.WaitForAudioDeviceToAppearAsync(new() { DeviceId = "missing", Flow = AudioFlow.Render }, 80);
        while (!wait.IsCompleted)
        {
            controller.ProcessDeviceNotification("unrelated");
            await Task.Delay(5);
        }
        Assert.False(await wait.WaitAsync(TimeSpan.FromSeconds(2)));
    }
}
