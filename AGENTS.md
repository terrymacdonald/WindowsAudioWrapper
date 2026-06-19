# AGENTS Guide for WindowsAudioWrapper

This file captures the essential rules and context for agents working on this WindowsAudioWrapper repository. 

## Project Scope
- Purpose: Safe C# wrapper over various Windows 10/11 Audio APIs to allow a developer to grab, apply and change audio devices, audio outputs, inputs, and audio settings programmaticvally. Built as a Windows x64 library targeting .NET 10.0 and higher.
- Structure: 
    - Root `WindowsAudioWrapper/` project,
    - `WindowsAudioWrapper.Tests/` xUnit native test suite,
    - `WindowsAudioWrapper.SampleApp/` same application showing how to use the library functionality

## API Design
  - The API provides simplified access to the underlying windows APIs via P/Invoke. The API is designed to hide away all of the complications of pointer management, handling multiple different APIs, and instead provides a single easy to use API to simply get and set various Windows 10/11 Audio settings.
  - The API MUST provide and consume DTO objects that are designed to be easily serialisable to and from JSON using Newtonsoft.Json, and must be able to handle consuming the DTOs after a reboot. This means that the DTOs cannot contain pointers, handles, or any other shortlived entities that would not survive a reboot. 
  - The library can use pointers, handles, or any other shortlived entities internally, but they must be able to be recreated internally by the library when consuming the DTOs through the API.

## API Functionality
The API developed must provide the following functionality:
- Getting and setting individual audio settings
- Getting and setting all the available audio settings as a single AudioProfile
- Providing a List<>  an individual audio setting that is an enum so that it can be easily consumed by the user

## Core Development Rules
- ALWAYS MAKE SURE THAT YOU TELL THE USER YOUR PLAN BEFORE YOU MAKE ANY CHANGES TO FILES AND GIVE THE USER A CHANCE TO REVIEW. ONLY MAKE CHANGS ONCE THE USER HAS GIVEN THEIR APPROVAL. The user can tell you to perform multiple steps of a plan if you want to.
- When PLANNING, if you think you will get confused and lose track of where you are in your plan, then please write it down into a planning document. Keep the planning document updated as you go, and make sure that the information you store in the planning document is very descriptive and detailed, so that if you lose track in the future you can review the planning document and you will know what to do and will do it well. Do not be overly concise as you lose a lot of nuance that will be important.
- DO NOT MAKE THINGS UP. Always check the Microsoft documentation and samples, other exmaple documentation and samples available on Github, or the WindowsAudioWrapper code in `WindowsAudioWrapper` if you need more information. If you are unsure then tell the user. The user wants you to only use facts - not conjecture. Tune your temperature to the lowest you can. You must be factual in your answers - DO NOT INVENT OR MAKE ANYTHING UP. ACCURACY IS THE MOST IMPORTANT THING.
- Write code that tries to be robust and cope with errors getting the information requested, but without causing an exception or a crash. 
- Naming/patterns: Preserve established coding patterns and styles across this project. Ask the user for permission if you need to deviate from those styles.
- Replicate existing helper/test patterns for new features. Consistency of the API is so very important. The user has spent a long time trying to keep everything standard and consistent, so make sure new creations align with existing patterns. Ask for permission for anything that does not align.
- Platform: Windows-only, x64; relies on various Windows Audio APIs, and potentially even emulation of the Windows Settings application to enable the functionality.
- Always make sure that the XML Comments that describe a function are always added to any functions and structs/enums so that XML documents can be generated for the DLL , so that they will be available in Intellisense when the VSCode or Visual Studio IDE is used.
- If you need to run scripts, note that all development is being done on Windows 11 x64 machines, and within Powershell terminals. You MUST make sure that your scripts will run in a powershell environment on a Windows 11 x64 machine.


## Other Development Rules
- Memory management: API objects should handle all underlying Native level memory management themselves. The user should not need to worry about it. This includes memory creation, disposal when objects are deleted, and handling functions being called multiple times in threads. Our aim is to never have memory leaks when using the API.

## Build and Scripts
- Prepare: `./prepare_windowsaudio.ps1` (prepares the repo for use with VSCode and/or Visual Studio).
- Build: `./build_windowsaudio.ps1` (restores, cleans, builds solution; version from `VERSION` + git commit count). Direct build: `dotnet build WindowsAudioWrapper/WindowsAudioWrapper.csproj`.
- Release ZIP: `./create_windowsaudio_release_zip.ps1` (produces artifacts/WindowsAudioWrapper-<version>-Release.zip).

## Testing Expectations
- CRITICAL: The tests are designed to find errors in the WindowsAudioWrapper library. DO NOT PATCH TESTS SO THAT THEY RUN SUCCESSFULLY TO AVOID UNDERLYING ERRORS IN THE WindowsAudioWrapper LIBRARY. THE WHOLE POINT OF TESTING IS TO FIND UNDERLYING ERRORS IN THE WindowsAudioWrapper LIBRARY SO THAT THEY CAN BE FIXED. Any tests for 
- Unsupported functionality: THere may be some features that are offered by the Windows Audio APIs  that are not supported by our tests hardware or the version of the driver we are using. ANY OPTIONAL FEATURES SHOULD BE GATED BY TESTS THAT CONFIRM THEIR SUPPORT BEFORE THEY ARE RUN. Unsupported features should be skipped using xUnit.Skip.
- Suites: xUnit in `WindowsAudioWrapper.Tests` targeting `net10.0`; Global xUnit parallelization is disabled.
- Test one feature per individual test case, as we want to keep good visibility for the user as to which test fails.
- Test Filenames should align with each of the feature areas being tested. 
  - This structure will keep the files small to make it easier for LLMs to edit them, and make it easier for humans to find the functions when they need fixing.

## Usage Notes
None.

## Versioning
- Version scheme: `VERSION` provides MAJOR.MINOR; PATCH computed from git commit count via `SetVersionFromGit` and `build_windowsaudio.ps1`. Update `VERSION` when bumping MAJOR/MINOR.

## Expectations for Agents
- Keep APIs and helpers consistent with existing conventions; avoid breaking established patterns. Consistentcy is key across the whole codebase. Do not deviate from this consistency without first requesting permission from the user. 
- Respect disposal and pointer ownership rules; ensure safe lifetime handling.
- Maintain optional-feature gating and hardware skip behavior in tests and helpers.
