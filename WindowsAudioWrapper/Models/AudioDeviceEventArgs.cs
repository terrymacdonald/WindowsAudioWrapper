namespace WindowsAudioWrapper.Models;

using System;

/// <summary>
/// Provides event data for Windows audio endpoint connection arrivals and hardware state changes.
/// </summary>
public sealed class AudioDeviceEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the unique native device identifier string.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the physical PnP hardware container identifier.
    /// </summary>
    public string ContainerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the immediate friendly description name of the endpoint.
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the fully qualified interface description name of the endpoint.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the system data flow direction (Render or Capture).
    /// </summary>
    public AudioFlow Flow { get; set; } = AudioFlow.Unknown;
}