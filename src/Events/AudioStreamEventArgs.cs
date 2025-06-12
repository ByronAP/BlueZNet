using BlueZNet.Models.Audio;
using BlueZNet.Models.Device;
using System;

namespace BlueZNet.Events
{
    /// <summary>
    /// Provides data for audio stream change events.
    /// </summary>
    public class AudioStreamEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStreamEventArgs"/> class.
        /// </summary>
        /// <param name="device">The device whose audio stream changed.</param>
        /// <param name="audioStream">The updated audio stream information.</param>
        /// <param name="timestamp">The timestamp when the audio stream event occurred.</param>
        public AudioStreamEventArgs(BluetoothDevice device, AudioStreamInfo audioStream, DateTime? timestamp = null)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            AudioStream = audioStream ?? throw new ArgumentNullException(nameof(audioStream));
            Timestamp = timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the device whose audio stream changed.
        /// </summary>
        public BluetoothDevice Device { get; }

        /// <summary>
        /// Gets the updated audio stream information.
        /// </summary>
        public AudioStreamInfo AudioStream { get; }

        /// <summary>
        /// Gets the timestamp when the audio stream event occurred.
        /// </summary>
        public DateTime Timestamp { get; }
    }
}
