using System.Collections.Generic;

namespace BlueZNet.Models.Capabilities
{
    /// <summary>
    /// Contains information about AVRCP capabilities and supported commands.
    /// </summary>
    public class AvrcpCapabilities
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AvrcpCapabilities"/> class.
        /// </summary>
        /// <param name="version">The AVRCP version supported by the device.</param>
        /// <param name="supportedCommands">The list of AVRCP commands supported by the device.</param>
        /// <param name="supportsPlayback">Whether basic playback control is supported.</param>
        /// <param name="supportsVolumeControl">Whether volume control is supported.</param>
        /// <param name="supportsBrowsing">Whether media browsing is supported.</param>
        /// <param name="supportsMetadata">Whether track metadata is supported.</param>
        /// <param name="supportsPosition">Whether position control (seeking) is supported.</param>
        public AvrcpCapabilities(
            string version = "Unknown",
            IReadOnlyList<string> supportedCommands = null,
            bool supportsPlayback = false,
            bool supportsVolumeControl = false,
            bool supportsBrowsing = false,
            bool supportsMetadata = false,
            bool supportsPosition = false)
        {
            Version = version ?? "Unknown";
            SupportedCommands = supportedCommands ?? new List<string>();
            SupportsPlayback = supportsPlayback;
            SupportsVolumeControl = supportsVolumeControl;
            SupportsBrowsing = supportsBrowsing;
            SupportsMetadata = supportsMetadata;
            SupportsPosition = supportsPosition;
        }

        /// <summary>
        /// Gets the AVRCP version supported by the device.
        /// </summary>
        /// <example>1.4, 1.6</example>
        public string Version { get; }

        /// <summary>
        /// Gets the list of AVRCP commands supported by the device.
        /// </summary>
        /// <example>play, pause, next, previous, volume</example>
        public IReadOnlyList<string> SupportedCommands { get; }

        /// <summary>
        /// Gets a value indicating whether basic playback control is supported.
        /// </summary>
        public bool SupportsPlayback { get; }

        /// <summary>
        /// Gets a value indicating whether volume control is supported.
        /// </summary>
        public bool SupportsVolumeControl { get; }

        /// <summary>
        /// Gets a value indicating whether media browsing is supported.
        /// </summary>
        public bool SupportsBrowsing { get; }

        /// <summary>
        /// Gets a value indicating whether track metadata is supported.
        /// </summary>
        public bool SupportsMetadata { get; }

        /// <summary>
        /// Gets a value indicating whether position control (seeking) is supported.
        /// </summary>
        public bool SupportsPosition { get; }
    }
}
