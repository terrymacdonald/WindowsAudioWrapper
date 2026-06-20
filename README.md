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
  "ProfileName": "Ultra-Fidelity Studio Monitor",
  "IsActive": true,
  "Playback": {
    "TargetDevice": {
      "DeviceId": "{0.0.0.00000000}.{a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d}",
      "ContainerId": "b4c5d6e7-f8a9-0b1c-2d3e-4f5a6b7c8d9e",
      "FriendlyName": "Scarlett 2i2 Out",
      "HardwareDetails": {
        "DeviceDescription": "Focusrite USB Audio",
        "HardwareId": "USB\\VID_1235&PID_8212",
        "DriverVersion": "4.119.13.0",
        "EndpointAssociationGuid": "38be3000-76f5-11d0-a292-00a0c9223196"
      }
    },
    "VolumePercent": 70.00,
    "IsMuted": false,
    "StreamFormat": {
      "SampleRate": 192000,
      "BitDepth": 24,
      "Channels": 2
    },
    "AudioEnhancements": {
      "AreEnhancementsSupported": true,
      "DisableAllEnhancements": false,
      "ActiveEffectsGuidsList": []
    }
  },
  "Recording": {
    "TargetDevice": {
      "DeviceId": "{0.0.10000000}.{capture-guid-here}",
      "ContainerId": "container-guid-here",
      "FriendlyName": "Scarlett 2i2 In",
      "HardwareDetails": {
        "DeviceDescription": "Focusrite USB Audio",
        "HardwareId": "USB\\VID_1235&PID_8212",
        "DriverVersion": "4.119.13.0",
        "EndpointAssociationGuid": "20889842-76f5-11d0-a292-00a0c9223196"
      }
    },
    "VolumePercent": 50.00,
    "IsMuted": false,
    "StreamFormat": {
      "SampleRate": 48000,
      "BitDepth": 24,
      "Channels": 2
    },
    "AudioEnhancements": {
      "AreEnhancementsSupported": false,
      "DisableAllEnhancements": false,
      "ActiveEffectsGuidsList": []
    }
  }
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

*Note: Real hardware integration tests modify system configurations sequentially and are explicitly configured with `DisableTestParallelization = true` across the test collection boundary to guarantee state-isolation and prevent race conditions.*
