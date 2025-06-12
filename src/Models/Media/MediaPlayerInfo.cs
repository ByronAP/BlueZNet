using System;

namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Contains information about a device's media player state and current track.
    /// </summary>
    public class MediaPlayerInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPlayerInfo"/> class.
        /// </summary>
        /// <param name="objectPath">The BlueZ D-Bus object path for this media player.</param>
        /// <param name="status">The current playback status.</param>
        /// <param name="track">Metadata about the currently playing track.</param>
        /// <param name="position">The current playback position in milliseconds.</param>
        /// <param name="shuffle">Whether shuffle mode is enabled.</param>
        /// <param name="repeat">The current repeat mode.</param>
        /// <param name="playlist">Information about the current playlist.</param>
        public MediaPlayerInfo(
            string objectPath,
            string status = null,
            TrackMetadata track = null,
            uint? position = null,
            bool? shuffle = null,
            string repeat = null,
            PlaylistInfo playlist = null)
        {
            ObjectPath = objectPath ?? throw new ArgumentNullException(nameof(objectPath));
            Status = status;
            Track = track;
            Position = position;
            Shuffle = shuffle;
            Repeat = repeat;
            Playlist = playlist;
        }

        /// <summary>
        /// Gets the BlueZ D-Bus object path for this media player.
        /// </summary>
        public string ObjectPath { get; }

        /// <summary>
        /// Gets the current playback status.
        /// </summary>
        /// <example>playing, paused, stopped</example>
        public string Status { get; }

        /// <summary>
        /// Gets metadata about the currently playing track.
        /// </summary>
        public TrackMetadata Track { get; }

        /// <summary>
        /// Gets the current playback position in milliseconds.
        /// </summary>
        public uint? Position { get; }

        /// <summary>
        /// Gets a value indicating whether shuffle mode is enabled.
        /// </summary>
        public bool? Shuffle { get; }

        /// <summary>
        /// Gets the current repeat mode.
        /// </summary>
        /// <example>off, singletrack, alltracks, group</example>
        public string Repeat { get; }

        /// <summary>
        /// Gets information about the current playlist, if available.
        /// </summary>
        public PlaylistInfo Playlist { get; }
    }
}