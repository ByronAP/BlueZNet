using BlueZNet.Models.Device;
using System;

namespace BlueZNet.Events
{
    /// <summary>
    /// Provides data for device connection/disconnection events.
    /// </summary>
    public class BluetoothConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothConnectionEventArgs"/> class.
        /// </summary>
        /// <param name="device">The device that connected or disconnected.</param>
        /// <param name="isConnected">Whether the device is now connected or disconnected.</param>
        /// <param name="timestamp">The timestamp when the connection event occurred.</param>
        public BluetoothConnectionEventArgs(BluetoothDevice device, bool isConnected, DateTime? timestamp = null)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            IsConnected = isConnected;
            Timestamp = timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the device that connected or disconnected.
        /// </summary>
        public BluetoothDevice Device { get; }

        /// <summary>
        /// Gets a value indicating whether the device is now connected (true) or disconnected (false).
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// Gets the timestamp when the connection event occurred.
        /// </summary>
        public DateTime Timestamp { get; }
    }
}
