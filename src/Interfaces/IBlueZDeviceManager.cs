using BlueZNet.Enums;
using BlueZNet.Events;
using BlueZNet.Models.Capabilities;
using BlueZNet.Models.Device;
using BlueZNet.Models.Media;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Interfaces
{
    /// <summary>
    /// Manages BlueZ device discovery, connection monitoring, and state management.
    /// Implement this interface to customize device management behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts BlueZ device management, allowing you to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Customize device discovery and connection monitoring</description></item>
    /// <item><description>Implement custom device capability detection</description></item>
    /// <item><description>Add device filtering and management policies</description></item>
    /// <item><description>Mock device operations for testing scenarios</description></item>
    /// <item><description>Integrate with alternative Bluetooth stacks</description></item>
    /// </list>
    /// </remarks>
    public interface IBlueZDeviceManager
    {
        /// <summary>
        /// Gets a list of currently connected Bluetooth devices.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A read-only list of connected devices with full state information.</returns>
        Task<IReadOnlyList<BluetoothDevice>> GetConnectedDevicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of all known Bluetooth devices (paired and remembered).
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A read-only list of all known devices.</returns>
        Task<IReadOnlyList<BluetoothDevice>> GetKnownDevicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets comprehensive capability information for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Detailed capabilities including supported profiles, codecs, and features.</returns>
        Task<DeviceCapabilities> GetDeviceCapabilitiesAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of Bluetooth profiles supported by the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of supported audio profiles.</returns>
        Task<IReadOnlyList<AudioProfile>> GetSupportedProfilesAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the currently active Bluetooth profile for the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The active profile, or null if unknown.</returns>
        Task<AudioProfile?> GetActiveProfileAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Switches the device to use a specific Bluetooth profile.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="profile">The profile to switch to.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the profile was switched successfully; otherwise, false.</returns>
        Task<bool> SwitchProfileAsync(string deviceAddress, AudioProfile profile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a specific feature is supported by the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="feature">The feature name to check (e.g., "volume", "browsing", "seek").</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the feature is supported; otherwise, false.</returns>
        Task<bool> IsFeatureSupportedAsync(string deviceAddress, string feature, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all media transports associated with the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of media transports for the device.</returns>
        Task<IReadOnlyList<MediaTransportInfo>> GetMediaTransportsAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts monitoring for device connection changes.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the monitoring operation.</param>
        /// <returns>A task representing the asynchronous monitoring operation.</returns>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops monitoring for device connection changes.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopMonitoringAsync();

        /// <summary>
        /// Occurs when a Bluetooth device connects or disconnects.
        /// </summary>
        event EventHandler<BluetoothConnectionEventArgs> DeviceConnectionChanged;
    }
}