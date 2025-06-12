using System;

namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Represents an individual media item (track, album, etc.).
    /// </summary>
    public class MediaItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaItem"/> class.
        /// </summary>
        /// <param name="objectPath">The BlueZ D-Bus object path for this item.</param>
        /// <param name="name">The display name of the item.</param>
        /// <param name="type">The type of media item.</param>
        /// <param name="playable">Whether this item can be played.</param>
        /// <param name="metadata">Metadata for this item.</param>
        public MediaItem(
            string objectPath,
            string name,
            string type,
            bool playable = false,
            TrackMetadata metadata = null)
        {
            ObjectPath = objectPath ?? throw new ArgumentNullException(nameof(objectPath));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Playable = playable;
            Metadata = metadata;
        }

        /// <summary>
        /// Gets the BlueZ D-Bus object path for this item.
        /// </summary>
        public string ObjectPath { get; }

        /// <summary>
        /// Gets the display name of the item.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of media item.
        /// </summary>
        /// <example>video, audio, folder</example>
        public string Type { get; }

        /// <summary>
        /// Gets a value indicating whether this item can be played.
        /// </summary>
        public bool Playable { get; }

        /// <summary>
        /// Gets metadata for this item, if available.
        /// </summary>
        public TrackMetadata Metadata { get; }
    }
}