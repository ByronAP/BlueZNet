using System;
using System.Collections.Generic;

namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Contains information about media browsing capabilities.
    /// </summary>
    public class MediaBrowserInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaBrowserInfo"/> class.
        /// </summary>
        /// <param name="objectPath">The BlueZ D-Bus object path for the media browser.</param>
        /// <param name="browsingSupported">Whether browsing is supported.</param>
        /// <param name="availableFolders">The list of available top-level folders.</param>
        public MediaBrowserInfo(
            string objectPath,
            bool browsingSupported = false,
            IReadOnlyList<MediaFolder> availableFolders = null)
        {
            ObjectPath = objectPath ?? throw new ArgumentNullException(nameof(objectPath));
            BrowsingSupported = browsingSupported;
            AvailableFolders = availableFolders ?? new List<MediaFolder>();
        }

        /// <summary>
        /// Gets the BlueZ D-Bus object path for the media browser.
        /// </summary>
        public string ObjectPath { get; }

        /// <summary>
        /// Gets a value indicating whether browsing is supported.
        /// </summary>
        public bool BrowsingSupported { get; }

        /// <summary>
        /// Gets the list of available top-level folders.
        /// </summary>
        public IReadOnlyList<MediaFolder> AvailableFolders { get; }
    }
}