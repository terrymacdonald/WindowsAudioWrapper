namespace WindowsAudioWrapper.Tests;

using WindowsAudioWrapper.Models;

public sealed class NotificationLifecycleTests
{
    [Fact]
    public void FailedRegistrationCanBeRetried()
    {
        EndpointTestProviders providers = new();
        using WindowsAudioController controller = providers.Controller();
        EventHandler<AudioDeviceEventArgs> handler = (_, _) => { };
        controller.AnyAudioDeviceConnected += handler;
        controller.AnyAudioDeviceConnected -= handler;
        providers.RegistrationSucceeds = true;
        controller.AnyAudioDeviceConnected += handler;
        controller.AnyAudioDeviceConnected += handler;
        Assert.Equal(2, providers.RegistrationAttempts);
    }

    [Fact]
    public void SubscriberFailureDoesNotPreventOtherSubscribers()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("speaker"));
        using WindowsAudioController controller = providers.Controller();
        int called = 0;
        controller.AnyAudioDeviceConnected += (_, _) => throw new InvalidOperationException("Synthetic subscriber error.");
        controller.AnyAudioDeviceConnected += (_, _) => called++;
        controller.ProcessDeviceNotification("speaker");
        Assert.Equal(1, called);
    }

    [Fact]
    public async Task TargetedSubscriberCanRegisterFromAnotherThreadWithoutInternalLockDeadlock()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("speaker"));
        using WindowsAudioController controller = providers.Controller();
        controller.RegisterTargetDevice(new() { DeviceId = "speaker", Flow = AudioFlow.Render });
        object registrationLock = typeof(WindowsAudioController).GetField("_notificationRegistrationLock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(controller)!;
        object targetLock = typeof(WindowsAudioController).GetField("_targetedDevicesLock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(controller)!;
        bool holdsInternalLock = false;
        Task? registration = null;
        controller.AudioDeviceConnected += (_, _) =>
        {
            registration = Task.Run(() => controller.RegisterTargetDevice(new() { DeviceId = "other", Flow = AudioFlow.Render }));
            holdsInternalLock = Monitor.IsEntered(registrationLock) || Monitor.IsEntered(targetLock);
        };
        controller.ProcessDeviceNotification("speaker");
        Assert.False(holdsInternalLock);
        await registration!.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void TargetedMicrophoneEventDoesNotMatchSpeakerOnTheSameContainer()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("speaker"));
        providers.Devices.Add(EndpointTestProviders.Device("microphone", AudioFlow.Capture));
        using WindowsAudioController controller = providers.Controller();
        int targeted = 0;
        controller.RegisterTargetDevice(new() { DeviceId = "old-speaker", ContainerId = "container", Flow = AudioFlow.Render });
        controller.AudioDeviceConnected += (_, _) => targeted++;
        controller.ProcessDeviceNotification("microphone");
        Assert.Equal(0, targeted);
        controller.ProcessDeviceNotification("speaker");
        Assert.Equal(1, targeted);
    }

    [Fact]
    public void QueuedNotificationAfterDisposalDoesNotReadProvidersOrNotify()
    {
        int reads = 0, calls = 0;
        EndpointTestProviders providers = new() { ReadInventory = () => { reads++; return new[] { EndpointTestProviders.Device("speaker") }; } };
        WindowsAudioController controller = providers.Controller();
        controller.AnyAudioDeviceConnected += (_, _) => calls++;
        controller.Dispose();
        controller.ProcessDeviceNotification("speaker");
        Assert.Equal(0, reads);
        Assert.Equal(0, calls);
        Assert.Throws<ObjectDisposedException>(() => controller.AnyAudioDeviceConnected += (_, _) => { });
    }

    [Fact]
    public void UnregisteringARecordingTargetPreservesAPlaybackTargetOnTheSameContainer()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("speaker"));
        providers.Devices.Add(EndpointTestProviders.Device("microphone", AudioFlow.Capture));
        using WindowsAudioController controller = providers.Controller();
        controller.RegisterTargetDevice(new() { ContainerId = "container", Flow = AudioFlow.Render });
        controller.RegisterTargetDevice(new() { ContainerId = "container", Flow = AudioFlow.Capture });
        controller.UnregisterTargetDevice(new() { ContainerId = "container", Flow = AudioFlow.Capture });
        List<string> arrivals = new(); controller.AudioDeviceConnected += (_, args) => arrivals.Add(args.DeviceId);
        controller.ProcessDeviceNotification("microphone"); controller.ProcessDeviceNotification("speaker");
        Assert.Equal(new[] { "speaker" }, arrivals);
    }
    [Fact]
    public void RegistrationExceptionLeavesTheControllerRetryable()
    {
        EndpointTestProviders providers = new();
        int attempts = 0;
        using WindowsAudioController controller = new(providers, providers, providers, providers, providers, providers,
            () => { if (++attempts == 1) throw new System.Runtime.InteropServices.COMException("Synthetic registration failure."); return true; });
        controller.AnyAudioDeviceConnected += (_, _) => { };
        controller.AnyAudioDeviceConnected += (_, _) => { };
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task CopiedWaitHandlerIsSafeAfterCancellationAndUnsubscription()
    {
        EndpointTestProviders providers = new(); providers.Devices.Add(EndpointTestProviders.Device("unrelated", container: "other"));
        using WindowsAudioController controller = providers.Controller();
        using CancellationTokenSource cancellation = new();
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        controller.AnyAudioDeviceConnected += (_, _) => { entered.Set(); release.Wait(TimeSpan.FromSeconds(5)); };
        Task<bool> wait = controller.WaitForAudioDeviceToAppearAsync(new() { DeviceId = "missing" }, 60000, cancellation.Token);
        using StringWriter diagnostics = new();
        using System.Diagnostics.TextWriterTraceListener listener = new(diagnostics);
        System.Diagnostics.Trace.Listeners.Add(listener);
        Task notification = Task.Run(() => controller.ProcessDeviceNotification("unrelated"));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(2)));
            cancellation.Cancel();
            Assert.False(await wait.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { release.Set(); }
        try
        {
            await notification.WaitAsync(TimeSpan.FromSeconds(2));
            System.Diagnostics.Trace.Flush();
            Assert.DoesNotContain("Subscriber failed", diagnostics.ToString());
        }
        finally { System.Diagnostics.Trace.Listeners.Remove(listener); }
    }

    [Fact]
    public async Task DisposalWaitsForAnInFlightNotificationProviderRead()
    {
        EndpointTestProviders providers = new();
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        providers.ReadInventory = () => { entered.Set(); release.Wait(TimeSpan.FromSeconds(5)); return new[] { EndpointTestProviders.Device("speaker") }; };
        WindowsAudioController controller = providers.Controller();
        Task notification = Task.Run(() => controller.ProcessDeviceNotification("speaker"));
        Task? disposal = null;
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(2)));
            disposal = Task.Run(controller.Dispose);
            Assert.False(providers.Disposed);
        }
        finally { release.Set(); }
        await notification.WaitAsync(TimeSpan.FromSeconds(2));
        await disposal!.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(providers.Disposed);
    }

}
