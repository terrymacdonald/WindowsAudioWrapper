# WindowsAudioWrapper

A high-performance, robust, production-grade .NET 10 library wrapper around Windows Core Audio (WASAPI), `PolicyConfig`, and native Windows registry subsystems. `WindowsAudioWrapper` allows developers to programmatically capture, validate, modify, and apply multimedia audio configurations across endpoints seamlessly.

This library is designed for enterprise integration scenarios—such as **DisplayMagician**—featuring a strict **zero-null architecture constraint** while producing clean, high-fidelity JSON profile serializations.

---

## 💎 Core Architecture

A high-performance framework coordinating core Windows audio configurations, this library is built using a decoupled provider model that bridges unmanaged COM surfaces with a safe, developer-friendly C# public contract. It provides two distinct pathways for system configuration:

1. **Direct Per-Feature Control (Programmatic Path):** Instantly adjust single settings (volume, mute, default routing) directly on hardware endpoints without initializing or managing profile state objects.
2. **Macro Profile-Based Control (State Path):** Capture the entire system audio state, serialize it to a pristine JSON file, or apply tailored configuration files down to the hardware on demand.

### Zero-Null DTO Constraint
To remain compliant with rigid consumer constraints, all Data Transfer Objects (DTOs) use non-nullable primitives. Granular profile control is managed implicitly through hidden automation control flags marked with `[JsonIgnore]`. Features can be toggled individually, or hydrated globally on deserialization using the built-in `.EnableAllFeatures()` hook.

---

## 🛠️ Feature Matrix

* **Endpoint Enumeration:** Deep traversal of active, disabled, unplugged, or missing Render (Playback) and Capture (Recording) IMMDevice endpoints.
* **Default Routing Engine:** Direct programmatic assignment of default Multimedia and Communications voice devices via native `IPolicyConfig` injection.
* **Hardware Volume & Mute:** Accurate volume scalar manipulation mapping between 0.00% and 100.00% with delta-tolerance rounding guards.
* **Audio Stream Formats:** Extract and update shared-mode stream specifications (Sample Rate, Channels, and Bit Depth) via `WAVEFORMATEX` binary marshalling.
* **Audio Processing Objects (APOs):** Programmatic toggling of system audio enhancement checkboxes without throwing unmanaged index faults.
* **System-Wide Accessibility:** Registry-level manipulation of global accessibility mixed-mono configuration switches (`AccessibilityMonoMixState`).

---

## 📂 Expected JSON Profile Schema

When profiles are captured or serialized, the schema output is perfectly clean and optimized. Internal framework monitoring variables are completely stripped out at the serialization boundary.

```json
{
  "Playback": {
    "MultimediaDevice": {
      "DeviceId": "{0.0.0.00000000}.{718f6171-d4b3-4f54-a8a9-fc7b94010da3}",
      "ContainerId": "83b8f97a-ed0e-589e-8f04-b0ed57d314f5",
      "FriendlyName": "BenQ SW2700 (NVIDIA High Definition Audio)",
      "HardwareDetails": {
        "DeviceDescription": "BenQ SW2700",
        "FormFactorCode": 9,
        "PhysicalSpeakersMask": 0,
        "FullRangeSpeakersMask": 0,
        "EndpointGuid": "{718F6171-D4B3-4F54-A8A9-FC7B94010DA3}",
        "DeviceFormatSummary": "FormatTag=1;Channels=2;SampleRate=48000;BitsPerSample=16;BlockAlign=4;AvgBytesPerSec=192000",
        "SupportsEventDrivenMode": true,
        "JackSubType": "{D1B9CC2A-F519-417F-91C9-55FA65481001}"
      },
      "ApoFxProperties": {}
    },
    "ConsoleDevice": {
      "DeviceId": "{0.0.0.00000000}.{718f6171-d4b3-4f54-a8a9-fc7b94010da3}",
      "ContainerId": "83b8f97a-ed0e-589e-8f04-b0ed57d314f5",
      "FriendlyName": "BenQ SW2700 (NVIDIA High Definition Audio)",
      "HardwareDetails": {
        "DeviceDescription": "BenQ SW2700",
        "FormFactorCode": 9,
        "PhysicalSpeakersMask": 0,
        "FullRangeSpeakersMask": 0,
        "EndpointGuid": "{718F6171-D4B3-4F54-A8A9-FC7B94010DA3}",
        "DeviceFormatSummary": "FormatTag=1;Channels=2;SampleRate=48000;BitsPerSample=16;BlockAlign=4;AvgBytesPerSec=192000",
        "SupportsEventDrivenMode": true,
        "JackSubType": "{D1B9CC2A-F519-417F-91C9-55FA65481001}"
      },
      "ApoFxProperties": {}
    },
    "CommunicationsDevice": {
      "DeviceId": "{0.0.0.00000000}.{881233af-9d4c-4b90-bbcc-7da40ffa45ac}",
      "ContainerId": "9839ceec-ca2e-5d56-bfde-10a8561d91af",
      "FriendlyName": "Speakers (Yeti Stereo Microphone)",
      "HardwareDetails": {
        "DeviceDescription": "Speakers",
        "FormFactorCode": 1,
        "PhysicalSpeakersMask": 0,
        "FullRangeSpeakersMask": 0,
        "EndpointGuid": "{881233AF-9D4C-4B90-BBCC-7DA40FFA45AC}",
        "DeviceFormatSummary": "FormatTag=65534;Channels=2;SampleRate=48000;BitsPerSample=16;BlockAlign=4;AvgBytesPerSec=192000",
        "SupportsEventDrivenMode": true,
        "JackSubType": "{DFF21CE1-F70F-11D0-B917-00A0C9223196}"
      },
      "ApoFxProperties": {}
    },
    "VolumePercent": 16.00,
    "IsMuted": false,
    "StreamFormat": {
      "SampleRate": 48000,
      "BitDepth": 16,
      "Channels": 2,
      "ChannelMask": 0,
      "SampleFormat": 1
    },
    "AudioEnhancements": {
      "AreEnhancementsSupported": false,
      "DisableAllEnhancements": false,
      "ActiveEffectsGuidsList": []
    },
    "VolumeLeft": 16.0,
    "VolumeRight": 16.0
  },
  "Recording": {
    "MultimediaDevice": {
      "DeviceId": "{0.0.1.00000000}.{7a7ef8ed-54e6-465a-9873-cbdfb07811db}",
      "ContainerId": "00000000-0000-0000-ffff-ffffffffffff",
      "FriendlyName": "Microphone Array (Realtek(R) Audio)",
      "HardwareDetails": {
        "DeviceDescription": "Microphone Array",
        "FormFactorCode": 4,
        "PhysicalSpeakersMask": 0,
        "FullRangeSpeakersMask": 0,
        "EndpointGuid": "{7A7EF8ED-54E6-465A-9873-CBDFB07811DB}",
        "DeviceFormatSummary": "FormatTag=1;Channels=2;SampleRate=48000;BitsPerSample=16;BlockAlign=4;AvgBytesPerSec=192000",
        "SupportsEventDrivenMode": true,
        "JackSubType": "{DFF21BE5-F70F-11D0-B917-00A0C9223196}"
      },
      "ApoFxProperties": {}
    },
    "ConsoleDevice": {
      "DeviceId": "{0.0.1.00000000}.{7a7ef8ed-54e6-465a-9873-cbdfb07811db}",
      "ContainerId": "00000000-0000-0000-ffff-ffffffffffff",
      "FriendlyName": "Microphone Array (Realtek(R) Audio)",
      "HardwareDetails": {
        "DeviceDescription": "Microphone Array",
        "FormFactorCode": 4,
        "PhysicalSpeakersMask": 0,
        "FullRangeSpeakersMask": 0,
        "EndpointGuid": "{7A7EF8ED-54E6-465A-9873-CBDFB07811DB}",
        "DeviceFormatSummary": "FormatTag=1;Channels=2;SampleRate=48000;BitsPerSample=16;BlockAlign=4;AvgBytesPerSec=192000",
        "SupportsEventDrivenMode": true,
        "JackSubType": "{DFF21BE5-F70F-11D0-B917-00A0C9223196}"
      },
      "ApoFxProperties": {}
    },
    "CommunicationsDevice": {
      "DeviceId": "{0.0.1.00000000}.{26355e0c-f824-4ce3-8f89-2a1997aee290}",
      "ContainerId": "f402a1c6-4dc2-5e35-b4e5-a347980e34ff",
      "FriendlyName": "Speakerphone (Brio 500)",
      "HardwareDetails": {
        "DeviceDescription": "Speakerphone",
        "FormFactorCode": 6,
        "PhysicalSpeakersMask": 0,
        "FullRangeSpeakersMask": 0,
        "EndpointGuid": "{26355E0C-F824-4CE3-8F89-2A1997AEE290}",
        "DeviceFormatSummary": "FormatTag=65534;Channels=2;SampleRate=48000;BitsPerSample=16;BlockAlign=4;AvgBytesPerSec=192000",
        "SupportsEventDrivenMode": true,
        "JackSubType": "{DFF21DE3-F70F-11D0-B917-00A0C9223196}"
      },
      "ApoFxProperties": {}
    },
    "VolumePercent": 0.0,
    "IsMuted": false,
    "StreamFormat": {
      "SampleRate": 48000,
      "BitDepth": 16,
      "Channels": 2,
      "ChannelMask": 0,
      "SampleFormat": 1
    },
    "AudioEnhancements": {
      "AreEnhancementsSupported": true,
      "DisableAllEnhancements": false,
      "ActiveEffectsGuidsList": [
        "6f64adbe-8211-11e2-8c70-2c27d7f001fa",
        "6f64adbf-8211-11e2-8c70-2c27d7f001fa"
      ]
    },
    "VolumeLeft": 0.0,
    "VolumeRight": 0.0
  },
  "EndpointVisibilities": [
    {
      "DeviceId": "{0.0.0.00000000}.{0e9cacd7-7aff-464b-a999-52daed4cf806}",
      "ContainerId": "e0b7cd08-48c7-41e4-b7ce-3030f0b735f4",
      "FriendlyName": "Headset (ThinkPad Thunderbolt 4 Dock USB Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{13a6a074-cd60-4509-89f8-e470cf6cc45c}",
      "ContainerId": "00000000-0000-0000-ffff-ffffffffffff",
      "FriendlyName": "Speakers (Realtek(R) Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{1696cebf-2bf6-44a3-8d7a-c0085f3e5bb6}",
      "ContainerId": "cd74a660-a135-5ea7-9942-511460c75bff",
      "FriendlyName": "DELL U2715H (NVIDIA High Definition Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{3973d7c5-caa9-444b-8ea6-4a2c86bde7ca}",
      "ContainerId": "2e0d9a1c-119d-11ee-82f2-806e6f6e6963",
      "FriendlyName": "",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{497d2f43-65fe-49d7-8604-a03618c4173e}",
      "ContainerId": "00000000-0000-0000-ffff-ffffffffffff",
      "FriendlyName": "Speakers (Realtek(R) Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{66427899-03d2-4c39-9efe-4e71f0b5c9d5}",
      "ContainerId": "0a1577b5-d46e-5ca2-943c-4adb4ccf3040",
      "FriendlyName": "NVIDIA Output (NVIDIA High Definition Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{718f6171-d4b3-4f54-a8a9-fc7b94010da3}",
      "ContainerId": "83b8f97a-ed0e-589e-8f04-b0ed57d314f5",
      "FriendlyName": "BenQ SW2700 (NVIDIA High Definition Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{755dcacf-a37d-4b0e-968a-b0d23e506d89}",
      "ContainerId": "b8ba6990-b753-5407-9b2b-af416b7daa57",
      "FriendlyName": "PHL 223V5 (NVIDIA High Definition Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{7c267ae3-e7ea-4571-9519-596eefacbfb8}",
      "ContainerId": "ed447683-cd62-5335-bd4b-bbb06171b217",
      "FriendlyName": "Headphones (Realtek(R) Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{7f67c397-608a-4434-ae41-5d8464fb1327}",
      "ContainerId": "52f3a6fe-b113-54ff-8b2d-cc927c71c0f6",
      "FriendlyName": "Headphones (Realtek(R) Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{881233af-9d4c-4b90-bbcc-7da40ffa45ac}",
      "ContainerId": "9839ceec-ca2e-5d56-bfde-10a8561d91af",
      "FriendlyName": "Speakers (Yeti Stereo Microphone)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{8b450b4e-bbc7-4392-bd92-72d81408cfe7}",
      "ContainerId": "2e0d9a1c-119d-11ee-82f2-806e6f6e6963",
      "FriendlyName": "",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{9a4db43f-5c85-4ff8-b79e-b6899a61fcc2}",
      "ContainerId": "f48ea52f-fe7f-5a87-afbc-5a1a87ba7745",
      "FriendlyName": "NVIDIA Output (NVIDIA High Definition Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.0.00000000}.{e163f34a-aac6-4492-a54c-42f8347ccc66}",
      "ContainerId": "c09bc615-f4ad-5541-a187-9a27d8bac7e6",
      "FriendlyName": "34GLR-H (NVIDIA High Definition Audio)",
      "Flow": 1,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.1.00000000}.{26355e0c-f824-4ce3-8f89-2a1997aee290}",
      "ContainerId": "f402a1c6-4dc2-5e35-b4e5-a347980e34ff",
      "FriendlyName": "Speakerphone (Brio 500)",
      "Flow": 2,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.1.00000000}.{7a7ef8ed-54e6-465a-9873-cbdfb07811db}",
      "ContainerId": "00000000-0000-0000-ffff-ffffffffffff",
      "FriendlyName": "Microphone Array (Realtek(R) Audio)",
      "Flow": 2,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.1.00000000}.{9687b016-2bf1-46ec-aeaa-f986d8cfa2ec}",
      "ContainerId": "e0b7cd08-48c7-41e4-b7ce-3030f0b735f4",
      "FriendlyName": "Microphone (ThinkPad Thunderbolt 4 Dock USB Audio)",
      "Flow": 2,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.1.00000000}.{9762d5d1-6dbe-4ee2-86ff-616bfe12f3b3}",
      "ContainerId": "9839ceec-ca2e-5d56-bfde-10a8561d91af",
      "FriendlyName": "Microphone (Yeti Stereo Microphone)",
      "Flow": 2,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.1.00000000}.{b13e5a11-f748-4cba-8f33-37275c69d9ab}",
      "ContainerId": "00000000-0000-0000-ffff-ffffffffffff",
      "FriendlyName": "Internal AUX Jack (NVIDIA High Definition Audio)",
      "Flow": 2,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.1.00000000}.{d60338b7-db6f-414a-ba6d-a415a022b426}",
      "ContainerId": "2e0d9a1c-119d-11ee-82f2-806e6f6e6963",
      "FriendlyName": "",
      "Flow": 2,
      "IsDisabled": false
    },
    {
      "DeviceId": "{0.0.1.00000000}.{e9f4d810-ff84-4076-8e79-02dc431acb98}",
      "ContainerId": "00000000-0000-0000-ffff-ffffffffffff",
      "FriendlyName": "Stereo Mix (Realtek(R) Audio)",
      "Flow": 2,
      "IsDisabled": true
    },
    {
      "DeviceId": "{0.0.1.00000000}.{ee2d80f0-218d-4c7c-b0da-c224ec3b2b07}",
      "ContainerId": "9839ceec-ca2e-5d56-bfde-10a8561d91af",
      "FriendlyName": "Microphone (Yeti Stereo Microphone)",
      "Flow": 2,
      "IsDisabled": false
    }
  ]
}
```

---

## 💻 API Usage Examples

### 1. Direct Per-Feature Pathway (No Profile Required)
For isolated hardware interactions where initializing state profiles is unnecessary, use the direct methods exposed on the controller facade.

```csharp
using WindowsAudioWrapper;
using WindowsAudioWrapper.Models;

// Initialize the root controller facade
using IWindowsAudioController controller = new WindowsAudioController();

string targetDeviceId = "{0.0.0.00000000}.{a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d}";

// 1. Adjust master hardware volume scalar directly
controller.SetVolumePercent(targetDeviceId, 75.5m);

// 2. Toggle hard mute line directly
controller.SetMute(targetDeviceId, false);

// 3. Update routing to establish device as system default multimedia renderer
controller.SetDefaultPlaybackDevice(targetDeviceId);

// 4. Configure stream specifications programmatically
var targetFormat = new AudioFormatProfile
{
    SampleRate = 96000,
    BitsPerSample = 24,
    Channels = 2
};
controller.SetFormat(targetDeviceId, targetFormat);
```

### 2. Full Macro Profile Pathway (JSON Interaction)
To capture the entire live audio environment, export it to disk, or apply configuration macros from external setup states.

```csharp
using System.IO;
using System.Text.Json;
using WindowsAudioWrapper;
using WindowsAudioWrapper.Models;

using IWindowsAudioController controller = new WindowsAudioController();

// --- Path A: Capture and Export ---
// Grabs full live environment variables natively
AudioProfile liveProfile = controller.GetCurrentProfile();

// Serialize cleanly to a JSON file format (Hides internal telemetry booleans)
string jsonOutput = JsonSerializer.Serialize(liveProfile, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("StudioMonitorProfile.json", jsonOutput);


// --- Path B: Import and Macro Apply ---
// Read clean JSON structure from disk
string jsonInput = File.ReadAllText("StudioMonitorProfile.json");
AudioProfile loadedProfile = JsonSerializer.Deserialize<AudioProfile>(jsonInput);

// CRITICAL: Hydrate the hidden control flags to true for full execution pass
loadedProfile.EnableAllFeatures();

// Execute business validation loops before executing hardware changes
AudioProfileValidationResult validation = controller.ValidateProfile(loadedProfile);
if (validation.Successful)
{
    // Apply macro configurations safely down onto unmanaged property stores
    AudioProfileApplyResult applyResult = controller.ApplyProfile(loadedProfile);
    if (applyResult.Successful)
    {
        System.Console.WriteLine("Audio Profile macro deployed successfully!");
    }
}
```

### 3. Selective/Granular Profile Pathway (Zero Nulls)
To programmatically apply *only specific features* via a profile object without violating zero-null restrictions, turn on individual hidden control flags manually.

```csharp
using WindowsAudioWrapper;
using WindowsAudioWrapper.Models;

using IWindowsAudioController controller = new WindowsAudioController();

// Initialize a fresh profile containing primitive zeros (No nulls used)
var selectiveProfile = new AudioProfile();

// 1. Isolate the target hardware by providing its anchor reference coordinates
selectiveProfile.Playback.TargetDevice.DeviceId = "{0.0.0.00000000}.{a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d}";
selectiveProfile.Playback.TargetDevice.IsEndpointEnabled = true;

// 2. Explicitly toggle the single feature block you wish to evaluate
selectiveProfile.Playback.IsPlaybackEnabled = true;
selectiveProfile.Playback.IsVolumeEnabled = true; // Flips volume modification on
selectiveProfile.Playback.VolumePercent = 65.00m;

// All other features (Mute, StreamFormat, APOs) remain flagged false.
// The engine will gracefully skip them, leaving current hardware defaults intact!
controller.ApplyProfile(selectiveProfile);
```

---

## 🚀 Building & Integration Testing

### Requirements
* **Operating System:** Windows 10 (Build 17763 / Version 1809) or higher.
* **SDK:** .NET 10.0 SDK or higher.

### Command Line Tools
The workspace includes automation scripts to coordinate restorations, cleans, structural compilations, and sequential testing.

* **Build the Solution:**
  ```powershell
  .\\build_windowsaudio.ps1
  ```
* **Execute the Hardware Integration Test Suite:**
  ```powershell
  .\\test_windowsaudio.ps1
  ```
  **Update the docfx documentation site, and run it for easy API review using a web browser:**
  ```powershell
  .\\refresh_windowsaudio_api_docs.ps1
  ```
  **Package up a new release zip file:**
  ```powershell
  .\\create_windowsaudio_release_zip.ps1
  ```

*Note: Real hardware integration tests modify system configurations sequentially and are explicitly configured with `DisableTestParallelization = true` across the test collection boundary to guarantee state-isolation and prevent race conditions.*
