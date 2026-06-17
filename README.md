# WindowsAudioWrapper

WindowsAudioWrapper is a Windows-only .NET wrapper library for reading and applying Windows audio settings using a simple profile-based API.

It is designed to provide a reusable audio control layer for applications such as DisplayMagician, while remaining general-purpose enough for other Windows projects.

The core design principle is simple:

```csharp
AudioProfile currentProfile = audioController.GetCurrentProfile();
audioController.ApplyProfile(profileToUse);
```

An `AudioProfile` is the only state object. It can represent a user-created profile, the current Windows audio state, a temporary profile used for rollback, or a profile loaded from JSON.

## Project goals

WindowsAudioWrapper aims to:

- provide a modern replacement for older .NET Framework audio libraries;
- support .NET 10 Windows applications;
- expose simple, JSON-friendly DTOs;
- avoid nullable child DTOs;
- use explicit `Is*Enabled` properties to control what settings are applied;
- keep Windows audio interop details out of consuming applications;
- support DisplayMagician-style apply/revert workflows without building DisplayMagician-specific concepts into the library.

## Target framework

The library is intended to target Windows only:

```xml
<TargetFramework>net10.0-windows</TargetFramework>
<Nullable>enable</Nullable>
<ImplicitUsings>enable</ImplicitUsings>
```

The sample application and xUnit test project should also target `net10.0-windows`.

## Recommended solution layout

```text
WindowsAudioWrapper/
├── WindowsAudioWrapper.slnx
├── WindowsAudioWrapper/
│   ├── WindowsAudioWrapper.csproj
│   ├── WindowsAudioController.cs
│   ├── IWindowsAudioController.cs
│   ├── Models/
│   ├── Providers/
│   └── Internal/
├── WindowsAudioWrapper.Tests/
│   ├── WindowsAudioWrapper.Tests.csproj
│   └── ...
└── WindowsAudioWrapper.SampleApp/
    ├── WindowsAudioWrapper.SampleApp.csproj
    └── Program.cs
```

The repository intentionally does not use `src`, `tests`, or `samples` folders. The sample app is kept at the solution root because there is only one sample application.

## Basic usage

```csharp
using WindowsAudioWrapper;
using WindowsAudioWrapper.Models;

using WindowsAudioController audioController = new();

AudioProfile currentProfile = audioController.GetCurrentProfile();

AudioProfile gameProfile = new()
{
    Playback =
    {
        IsPlaybackEnabled = true,
        IsDefaultPlaybackDeviceEnabled = true,
        IsDefaultCommunicationsPlaybackDeviceEnabled = true,
        IsVolumeEnabled = true,
        VolumePercent = 80,
        Device =
        {
            DeviceId = "{playback-device-id}",
            FriendlyName = "LG TV"
        },
        CommunicationsDevice =
        {
            DeviceId = "{playback-device-id}",
            FriendlyName = "LG TV"
        }
    },
    Recording =
    {
        IsRecordingEnabled = true,
        IsDefaultRecordingDeviceEnabled = true,
        IsVolumeEnabled = true,
        VolumePercent = 90,
        Device =
        {
            DeviceId = "{recording-device-id}",
            FriendlyName = "USB Microphone"
        }
    }
};

AudioProfileValidationResult validationResult = audioController.ValidateProfile(gameProfile);

if (validationResult.Successful)
{
    AudioProfileApplyResult applyResult = audioController.ApplyProfile(gameProfile);

    if (!applyResult.Successful)
    {
        foreach (AudioOperationMessage message in applyResult.Messages)
        {
            Console.WriteLine($"{message.Severity}: {message.Message}");
        }
    }
}

// Later, if the application wants to revert audio settings:
audioController.ApplyProfile(currentProfile);
```

## Profiles-only design

WindowsAudioWrapper deliberately does not expose separate snapshot or restore objects.

Instead, the library uses `AudioProfile` for everything:

- a profile created by a user;
- the current Windows audio state;
- a temporary profile used by an application to revert changes;
- a profile stored in JSON;
- a profile created by a test or sample app.

The public API is therefore intentionally small:

```csharp
AudioProfile GetCurrentProfile();
AudioProfileValidationResult ValidateProfile(AudioProfile profile);
AudioProfileApplyResult ApplyProfile(AudioProfile profile);
```

`GetCurrentProfile()` captures every setting that WindowsAudioWrapper can read at the time it is called. Settings that cannot be read are left disabled in the returned profile.

`ApplyProfile()` applies every setting whose corresponding `Is*Enabled` flag is enabled.

## DTO design

DTOs are designed to be JSON-friendly and stable.

Child DTO properties should always return non-null objects. The library uses explicit `Is*Enabled` fields to determine whether a setting should be applied.

For example:

```csharp
public sealed class AudioProfile
{
    public int SchemaVersion { get; set; } = 1;

    public PlaybackAudioProfile Playback { get; set; } = new();
    public RecordingAudioProfile Recording { get; set; } = new();
    public SystemAudioProfile System { get; set; } = new();
}
```

A disabled setting should be represented like this:

```csharp
profile.Playback.IsVolumeEnabled = false;
```

not like this:

```csharp
profile.Playback = null;
```

This makes profiles safer to serialize, deserialize, copy, edit, and store in application configuration files.

## Playback profile

A playback profile can describe default playback, communications playback, volume, mute, format, spatial sound, and audio enhancements.

```csharp
PlaybackAudioProfile playback = new()
{
    IsPlaybackEnabled = true,

    IsDefaultPlaybackDeviceEnabled = true,
    Device =
    {
        DeviceId = "{default-playback-device-id}",
        FriendlyName = "Speakers"
    },

    IsDefaultCommunicationsPlaybackDeviceEnabled = true,
    CommunicationsDevice =
    {
        DeviceId = "{communications-playback-device-id}",
        FriendlyName = "Headset"
    },

    IsVolumeEnabled = true,
    VolumePercent = 50,

    IsMuteEnabled = true,
    IsMuted = false
};
```

`Device` and `CommunicationsDevice` are separate because Windows can use different endpoints for default audio and default communications audio.

## Recording profile

A recording profile mirrors playback, but for capture devices such as microphones.

```csharp
RecordingAudioProfile recording = new()
{
    IsRecordingEnabled = true,

    IsDefaultRecordingDeviceEnabled = true,
    Device =
    {
        DeviceId = "{default-recording-device-id}",
        FriendlyName = "USB Microphone"
    },

    IsDefaultCommunicationsRecordingDeviceEnabled = true,
    CommunicationsDevice =
    {
        DeviceId = "{communications-recording-device-id}",
        FriendlyName = "Webcam Microphone"
    },

    IsVolumeEnabled = true,
    VolumePercent = 75,

    IsMuteEnabled = true,
    IsMuted = false
};
```

## System audio profile

System audio settings are represented separately from playback and recording endpoints.

For example, mono audio is a system/accessibility setting rather than a device-specific endpoint setting.

```csharp
profile.System.IsSystemAudioEnabled = true;
profile.System.IsMonoAudioEnabled = true;
profile.System.MonoAudio = false;
```

## Device identity

Profiles should prefer Windows endpoint device IDs for reliable matching.

Friendly names are still useful for display and fallback behaviour, but they should not be treated as the primary identifier.

```csharp
public sealed class AudioEndpointReference
{
    public string DeviceId { get; set; } = "";
    public string ContainerId { get; set; } = "";
    public string FriendlyName { get; set; } = "";
    public string FullName { get; set; } = "";
    public AudioFlow Flow { get; set; } = AudioFlow.Unknown;
}
```

## DisplayMagician-style workflow

Applications that need temporary audio changes can capture the current profile, apply another profile, and then apply the captured profile later.

```csharp
AudioProfile originalAudioProfile = audioController.GetCurrentProfile();

AudioProfileApplyResult applyResult = audioController.ApplyProfile(shortcut.AudioSettings.Profile);

if (!applyResult.Successful)
{
    // Show or log errors.
}

// Run the game, application, or executable.

if (shortcut.AudioSettings.ShouldRevertAudio)
{
    audioController.ApplyProfile(originalAudioProfile);
}
```

The rollback concept belongs to the consuming application, not to WindowsAudioWrapper. WindowsAudioWrapper only knows how to get and apply profiles.

## Validation and results

Normal audio problems should be returned as structured result messages rather than thrown as exceptions.

Examples include:

- selected endpoint not found;
- selected endpoint disabled;
- selected endpoint unplugged;
- setting unsupported by the current provider;
- audio format unsupported;
- spatial sound not supported;
- audio enhancements not readable or not settable.

Exceptions should be reserved for programmer errors or unexpected failures.

## Current implementation status

This initial library skeleton contains the public API, DTOs, result types, provider interfaces, and placeholder provider implementations.

The low-level Windows Core Audio and PolicyConfig interop still needs to be implemented.

Recommended first implementation milestone:

- enumerate playback endpoints;
- enumerate recording endpoints;
- read default playback endpoint;
- read default recording endpoint;
- read default communications playback endpoint;
- read default communications recording endpoint;
- set default playback endpoint;
- set default recording endpoint;
- set default communications playback endpoint;
- set default communications recording endpoint;
- read and set endpoint volume;
- read and set endpoint mute;
- populate `GetCurrentProfile()` with all supported v1 settings.

Recommended later milestones:

- read/test audio format;
- set audio format if reliable;
- read/apply spatial sound if reliable;
- read/apply audio enhancements if reliable;
- read/apply mono audio if reliable;
- read/apply microphone voice processing if supported.

## Notes on Windows APIs

WindowsAudioWrapper is expected to use Windows Core Audio APIs internally. Some Windows audio operations, such as reading endpoint volume, are available through documented APIs. Other operations, such as changing the system default audio endpoint, commonly require Windows policy configuration COM interfaces that are not part of the same stable public API surface.

The wrapper should report capabilities and failures clearly so consuming applications can decide what to show in their UI.

## License

Add the appropriate license for your project here.
