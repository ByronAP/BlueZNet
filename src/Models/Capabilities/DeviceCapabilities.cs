using BlueZNet.Enums;
using System.Collections.Generic;

namespace BlueZNet.Models.Capabilities
{
    /// <summary>
    /// Contains comprehensive information about a device's Bluetooth capabilities.
    /// </summary>
    public class DeviceCapabilities
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceCapabilities"/> class.
        /// </summary>
        /// <param name="avrcp">The AVRCP capabilities.</param>
        /// <param name="a2dp">The A2DP capabilities.</param>
        /// <param name="supportedProfiles">The list of Bluetooth profiles supported by this device.</param>
        /// <param name="supportsAbsoluteVolume">Whether the device supports AVRCP absolute volume control.</param>
        /// <param name="supportsBrowsing">Whether the device supports media browsing.</param>
        /// <param name="supportsSearch">Whether the device supports media search functionality.</param>
        public DeviceCapabilities(
            AvrcpCapabilities avrcp = null,
            A2dpCapabilities a2dp = null,
            IReadOnlyList<AudioProfile> supportedProfiles = null,
            bool supportsAbsoluteVolume = false,
            bool supportsBrowsing = false,
            bool supportsSearch = false)
        {
            Avrcp = avrcp ?? new AvrcpCapabilities();
            A2dp = a2dp ?? new A2dpCapabilities();
            SupportedProfiles = supportedProfiles ?? new List<AudioProfile>();
            SupportsAbsoluteVolume = supportsAbsoluteVolume;
            SupportsBrowsing = supportsBrowsing;
            SupportsSearch = supportsSearch;
        }

        /// <summary>
        /// Gets the AVRCP (Audio/Video Remote Control Profile) capabilities.
        /// </summary>
        public AvrcpCapabilities Avrcp { get; }

        /// <summary>
        /// Gets the A2DP (Advanced Audio Distribution Profile) capabilities.
        /// </summary>
        public A2dpCapabilities A2dp { get; }

        /// <summary>
        /// Gets the list of Bluetooth profiles supported by this device.
        /// </summary>
        public IReadOnlyList<AudioProfile> SupportedProfiles { get; }

        /// <summary>
        /// Gets a value indicating whether the device supports AVRCP absolute volume control.
        /// </summary>
        /// <remarks>
        /// When true, volume changes on the Pi will be reflected on the device and vice versa.
        /// </remarks>
        public bool SupportsAbsoluteVolume { get; }

        /// <summary>
        /// Gets a value indicating whether the device supports media browsing.
        /// </summary>
        /// <remarks>
        /// When true, you can browse the device's music library, playlists, and folders.
        /// </remarks>
        public bool SupportsBrowsing { get; }

        /// <summary>
        /// Gets a value indicating whether the device supports media search functionality.
        /// </summary>
        public bool SupportsSearch { get; }
    }
}
