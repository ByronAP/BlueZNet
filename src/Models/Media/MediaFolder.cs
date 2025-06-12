using System;

namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Represents a folder in the device's media library.
    /// </summary>
    public class MediaFolder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFolder"/> class.
        /// </summary>
        /// <param name="objectPath">The BlueZ D-Bus object path for this folder.</param>
        /// <param name="name">The display name of the folder.</param>
        /// <param name="type">The type of content in this folder.</param>
        /// <param name="playable">Whether this folder can be played directly.</param>
        /// <param name="numberOfItems">The number of items in this folder.</param>
        public MediaFolder(
            string objectPath,
            string name,
            string type,
            bool playable = false,
            uint? numberOfItems = null)
        {
            ObjectPath = objectPath ?? throw new ArgumentNullException(nameof(objectPath));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Playable = playable;
            NumberOfItems = numberOfItems;
        }

        /// <summary>
        /// Gets the BlueZ D-Bus object path for this folder.
        /// </summary>
        public string ObjectPath { get; }

        /// <summary>
        /// Gets the display name of the folder.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of content in this folder.
        /// </summary>
        /// <example>mixed, titles, albums, artists, playlists</example>
        public string Type { get; }

        /// <summary>
        /// Gets a value indicating whether this folder can be played directly.
        /// </summary>
        public bool Playable { get; }

        /// <summary>
        /// Gets the number of items in this folder, if known.
        /// </summary>
        public uint? NumberOfItems { get; }
    }
}
