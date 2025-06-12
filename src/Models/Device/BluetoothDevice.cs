using BlueZNet.Enums;
using BlueZNet.Models.Audio;
using BlueZNet.Models.Capabilities;
using BlueZNet.Models.Media;
using System;

namespace BlueZNet.Models.Device
{
    /// <summary>
    /// Represents a Bluetooth device with comprehensive information about its capabilities, connection status, and media state.
    /// </summary>
    public class BluetoothDevice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothDevice"/> class.
        /// </summary>
        /// <param name="address">The MAC address of the Bluetooth device.</param>
        /// <param name="name">The friendly name of the device.</param>
        /// <param name="objectPath">The BlueZ D-Bus object path for this device.</param>
        /// <param name="connected">Whether the device is currently connected.</param>
        /// <param name="lastSeen">The timestamp when this device was last seen or updated.</param>
        /// <param name="mediaPlayer">Information about the device's media player.</param>
        /// <param name="audioStream">Information about the device's audio stream.</param>
        /// <param name="mediaBrowser">Information about the device's media browsing capabilities.</param>
        /// <param name="capabilities">The detected capabilities of this device.</param>
        /// <param name="activeProfile">The currently active audio profile for this device.</param>
        public BluetoothDevice(
            string address,
            string name,
            string objectPath,
            bool connected,
            DateTime? lastSeen = null,
            MediaPlayerInfo mediaPlayer = null,
            AudioStreamInfo audioStream = null,
            MediaBrowserInfo mediaBrowser = null,
            DeviceCapabilities capabilities = null,
            AudioProfile? activeProfile = null)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ObjectPath = objectPath ?? throw new ArgumentNullException(nameof(objectPath));
            Connected = connected;
            LastSeen = lastSeen ?? DateTime.UtcNow;
            MediaPlayer = mediaPlayer;
            AudioStream = audioStream;
            MediaBrowser = mediaBrowser;
            Capabilities = capabilities ?? new DeviceCapabilities();
            ActiveProfile = activeProfile;
        }

        /// <summary>
        /// Gets the MAC address of the Bluetooth device.
        /// </summary>
        /// <example>AA:BB:CC:DD:EE:FF</example>
        public string Address { get; }

        /// <summary>
        /// Gets the friendly name of the device as reported by the device or set by the user.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the BlueZ D-Bus object path for this device.
        /// </summary>
        /// <remarks>
        /// This path is used internally for D-Bus communication with BlueZ.
        /// </remarks>
        public string ObjectPath { get; }

        /// <summary>
        /// Gets a value indicating whether the device is currently connected.
        /// </summary>
        public bool Connected { get; }

        /// <summary>
        /// Gets the timestamp when this device was last seen or updated.
        /// </summary>
        public DateTime LastSeen { get; }

        /// <summary>
        /// Gets information about the device's media player, if available.
        /// </summary>
        /// <remarks>
        /// This will be null if the device doesn't support AVRCP or doesn't have an active media player.
        /// </remarks>
        public MediaPlayerInfo MediaPlayer { get; }

        /// <summary>
        /// Gets information about the device's audio stream, if active.
        /// </summary>
        /// <remarks>
        /// This contains PulseAudio sink information and is only available when audio is being streamed.
        /// </remarks>
        public AudioStreamInfo AudioStream { get; }

        /// <summary>
        /// Gets information about the device's media browsing capabilities.
        /// </summary>
        /// <remarks>
        /// This will be null if the device doesn't support media browsing or if no folders are available.
        /// </remarks>
        public MediaBrowserInfo MediaBrowser { get; }

        /// <summary>
        /// Gets the detected capabilities of this device.
        /// </summary>
        /// <remarks>
        /// Capabilities are detected when the device connects and include supported profiles, codecs, and features.
        /// </remarks>
        public DeviceCapabilities Capabilities { get; }

        /// <summary>
        /// Gets the currently active audio profile for this device.
        /// </summary>
        /// <remarks>
        /// This indicates which Bluetooth profile is currently being used (A2DP, HFP, HSP, etc.).
        /// </remarks>
        public AudioProfile? ActiveProfile { get; }

        /// <summary>
        /// Creates a new instance with updated properties.
        /// </summary>
        /// <returns>A new <see cref="BluetoothDevice"/> instance with the specified properties updated.</returns>
        public BluetoothDevice WithUpdatedProperties(
            bool? connected = null,
            DateTime? lastSeen = null,
            MediaPlayerInfo mediaPlayer = null,
            AudioStreamInfo audioStream = null,
            MediaBrowserInfo mediaBrowser = null,
            DeviceCapabilities capabilities = null,
            AudioProfile? activeProfile = null)
        {
            return new BluetoothDevice(
                Address,
                Name,
                ObjectPath,
                connected ?? Connected,
                lastSeen ?? LastSeen,
                mediaPlayer ?? MediaPlayer,
                audioStream ?? AudioStream,
                mediaBrowser ?? MediaBrowser,
                capabilities ?? Capabilities,
                activeProfile ?? ActiveProfile);
        }
    }
}