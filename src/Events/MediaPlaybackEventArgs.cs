using BlueZNet.Models.Device;
using BlueZNet.Models.Media;
using System;

namespace BlueZNet.Events
{
    /// <summary>
    /// Provides data for media playback state change events.
    /// </summary>
    public class MediaPlaybackEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPlaybackEventArgs"/> class.
        /// </summary>
        /// <param name="device">The device whose media state changed.</param>
        /// <param name="mediaPlayer">The updated media player information.</param>
        /// <param name="timestamp">The timestamp when the media event occurred.</param>
        public MediaPlaybackEventArgs(BluetoothDevice device, MediaPlayerInfo mediaPlayer, DateTime? timestamp = null)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            MediaPlayer = mediaPlayer ?? throw new ArgumentNullException(nameof(mediaPlayer));
            Timestamp = timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the device whose media state changed.
        /// </summary>
        public BluetoothDevice Device { get; }

        /// <summary>
        /// Gets the updated media player information.
        /// </summary>
        public MediaPlayerInfo MediaPlayer { get; }

        /// <summary>
        /// Gets the timestamp when the media event occurred.
        /// </summary>
        public DateTime Timestamp { get; }
    }
}
