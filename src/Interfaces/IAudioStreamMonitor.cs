using BlueZNet.Events;
using BlueZNet.Models.Capabilities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Interfaces
{
    /// <summary>
    /// Monitors audio stream changes and manages audio processing settings.
    /// Implement this interface to customize audio monitoring behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts audio stream monitoring, allowing you to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Customize audio stream monitoring intervals and strategies</description></item>
    /// <item><description>Implement alternative volume control mechanisms</description></item>
    /// <item><description>Add audio codec management and optimization</description></item>
    /// <item><description>Mock audio monitoring for testing scenarios</description></item>
    /// <item><description>Integrate with different audio processing systems</description></item>
    /// </list>
    /// </remarks>
    public interface IAudioStreamMonitor
    {
        /// <summary>
        /// Starts monitoring audio streams for all connected devices.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the monitoring operation.</param>
        /// <returns>A task representing the asynchronous monitoring operation.</returns>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops monitoring audio streams.
        /// </summary>
        /// <returns>A task representing the asynchronous stop operation.</returns>
        Task StopMonitoringAsync();

        /// <summary>
        /// Sets the volume for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="volume">The volume level between 0.0 (silent) and 1.0 (maximum).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the volume was set successfully; otherwise, false.</returns>
        Task<bool> SetVolumeAsync(string deviceAddress, double volume, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the mute state for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="muted">True to mute the device; false to unmute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the mute state was set successfully; otherwise, false.</returns>
        Task<bool> SetMutedAsync(string deviceAddress, bool muted, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current volume level for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The volume level between 0.0 and 1.0, or null if unavailable.</returns>
        Task<double?> GetVolumeAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to set the audio codec used for the device connection.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="codecName">The name of the codec to use (e.g., "aptX", "AAC", "SBC").</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the codec change was initiated; otherwise, false.</returns>
        Task<bool> SetAudioCodecAsync(string deviceAddress, string codecName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed information about the audio codecs supported and in use by the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A2DP capability information including supported codecs.</returns>
        Task<A2dpCapabilities> GetAudioCodecInfoAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Occurs when a device's audio stream state changes.
        /// </summary>
        event EventHandler<AudioStreamEventArgs> AudioStreamChanged;
    }
}