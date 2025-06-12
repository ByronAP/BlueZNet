using BlueZNet.Enums;
using BlueZNet.Events;
using BlueZNet.Models.Audio;
using BlueZNet.Models.Capabilities;
using BlueZNet.Models.Device;
using BlueZNet.Models.Media;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Interfaces
{
    /// <summary>
    /// Provides comprehensive Bluetooth device monitoring and control capabilities.
    /// </summary>
    /// <remarks>
    /// This interface supports monitoring device connections, controlling media playback,
    /// managing audio processing, and browsing device media libraries when supported.
    /// </remarks>
    public interface IBlueZNetController : IDisposable
    {
        /// <summary>
        /// Occurs when a Bluetooth device connects or disconnects.
        /// </summary>
        event EventHandler<BluetoothConnectionEventArgs> DeviceConnectionChanged;

        /// <summary>
        /// Occurs when a device's media playback state changes.
        /// </summary>
        /// <remarks>
        /// This includes play/pause state changes, track changes, and metadata updates.
        /// </remarks>
        event EventHandler<MediaPlaybackEventArgs> MediaPlaybackChanged;

        /// <summary>
        /// Occurs when a device's audio stream state changes.
        /// </summary>
        /// <remarks>
        /// This includes volume changes, mute state changes, and audio format changes.
        /// </remarks>
        event EventHandler<AudioStreamEventArgs> AudioStreamChanged;

        /// <summary>
        /// Gets a value indicating whether the controller is currently active.
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// Starts monitoring Bluetooth devices and events.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the startup operation.</param>
        /// <returns>A task representing the asynchronous startup operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if monitoring is already active.</exception>
        /// <exception cref="BlueZNet.Exceptions.BlueZNetException">Thrown if BlueZ or D-Bus connection fails.</exception>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops monitoring Bluetooth devices and releases resources.
        /// </summary>
        /// <returns>A task representing the asynchronous shutdown operation.</returns>
        Task StopMonitoringAsync();

        /// <summary>
        /// Gets a list of currently connected Bluetooth devices.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A read-only list of connected devices with full state information.</returns>
        Task<IReadOnlyList<BluetoothDevice>> GetConnectedDevicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of all known Bluetooth devices (paired and remembered).
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A read-only list of all known devices.</returns>
        Task<IReadOnlyList<BluetoothDevice>> GetKnownDevicesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed audio stream information for a specific device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Audio stream information, or null if no active stream.</returns>
        Task<AudioStreamInfo> GetAudioStreamInfoAsync(string deviceAddress, CancellationToken cancellationToken = default);

        // Media Control Methods

        /// <summary>
        /// Starts or resumes playback on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
        /// <remarks>
        /// This method requires the device to support AVRCP playback commands.
        /// Use <see cref="IsFeatureSupportedAsync"/> to check capability first.
        /// </remarks>
        Task<bool> PlayAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses playback on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
        Task<bool> PauseAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops playback on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
        Task<bool> StopAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Skips to the next track on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
        Task<bool> NextTrackAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Goes to the previous track on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
        Task<bool> PreviousTrackAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Fast forwards the current track on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
        Task<bool> FastForwardAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rewinds the current track on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
        Task<bool> RewindAsync(string deviceAddress, CancellationToken cancellationToken = default);

        // Position/Seeking Control

        /// <summary>
        /// Seeks to a specific position in the current track.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="positionMs">The position to seek to, in milliseconds.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the seek was successful; otherwise, false.</returns>
        /// <remarks>
        /// This requires the device to support AVRCP position control.
        /// </remarks>
        Task<bool> SeekAsync(string deviceAddress, uint positionMs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current playback position of the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The current position in milliseconds, or null if unavailable.</returns>
        Task<uint?> GetPositionAsync(string deviceAddress, CancellationToken cancellationToken = default);

        // Volume Control

        /// <summary>
        /// Sets the volume for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="volume">The volume level between 0.0 (silent) and 1.0 (maximum).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the volume was set successfully; otherwise, false.</returns>
        /// <remarks>
        /// This method attempts both AVRCP absolute volume control (on the device)
        /// and PulseAudio sink volume control (on the Pi) for best compatibility.
        /// </remarks>
        Task<bool> SetVolumeAsync(string deviceAddress, double volume, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the mute state for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="muted">True to mute the device; false to unmute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the mute state was set successfully; otherwise, false.</returns>
        Task<bool> SetMutedAsync(string deviceAddress, bool muted, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current volume level for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The volume level between 0.0 and 1.0, or null if unavailable.</returns>
        Task<double?> GetVolumeAsync(string deviceAddress, CancellationToken cancellationToken = default);

        // Playback Mode Control

        /// <summary>
        /// Sets the shuffle mode for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="shuffle">True to enable shuffle; false to disable.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the shuffle mode was set successfully; otherwise, false.</returns>
        Task<bool> SetShuffleAsync(string deviceAddress, bool shuffle, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the repeat mode for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="mode">The repeat mode: "off", "singletrack", "alltracks", or "group".</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the repeat mode was set successfully; otherwise, false.</returns>
        Task<bool> SetRepeatModeAsync(string deviceAddress, string mode, CancellationToken cancellationToken = default);

        // Media Browsing & Playlist Control

        /// <summary>
        /// Browses the media folders available on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="folderPath">The path to browse, or null for top-level folders.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of available media folders.</returns>
        /// <remarks>
        /// Media browsing is not supported by all devices. YouTube Music, for example,
        /// does not expose its library via AVRCP browsing.
        /// </remarks>
        Task<IReadOnlyList<MediaFolder>> BrowseMediaAsync(string deviceAddress, string folderPath = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the items within a specific media folder.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="folderPath">The D-Bus object path of the folder to browse.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of media items in the folder.</returns>
        Task<IReadOnlyList<MediaItem>> GetFolderItemsAsync(string deviceAddress, string folderPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Plays a specific media item on the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="itemPath">The D-Bus object path of the item to play.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the item was started successfully; otherwise, false.</returns>
        Task<bool> PlayItemAsync(string deviceAddress, string itemPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a media item to the current playback queue.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="itemPath">The D-Bus object path of the item to queue.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the item was queued successfully; otherwise, false.</returns>
        Task<bool> AddToQueueAsync(string deviceAddress, string itemPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about the current playlist on the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Playlist information, or null if no playlist is active or available.</returns>
        Task<PlaylistInfo> GetCurrentPlaylistAsync(string deviceAddress, CancellationToken cancellationToken = default);

        // Device Capabilities & Profile Management

        /// <summary>
        /// Gets comprehensive capability information for the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Detailed capabilities including supported profiles, codecs, and features.</returns>
        /// <remarks>
        /// This method provides information about what the device can do, allowing you to
        /// check capabilities before attempting operations that might not be supported.
        /// </remarks>
        Task<DeviceCapabilities> GetDeviceCapabilitiesAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of Bluetooth profiles supported by the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of supported audio profiles.</returns>
        Task<IReadOnlyList<AudioProfile>> GetSupportedProfilesAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the currently active Bluetooth profile for the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The active profile, or null if unknown.</returns>
        Task<AudioProfile?> GetActiveProfileAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Switches the device to use a specific Bluetooth profile.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="profile">The profile to switch to.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the profile was switched successfully; otherwise, false.</returns>
        /// <remarks>
        /// <para>Profile switching is useful for different use cases:</para>
        /// <para>- A2DP: High-quality music streaming</para>
        /// <para>- HFP: Hands-free calling with audio</para>
        /// <para>- HSP: Basic headset functionality</para>
        /// </remarks>
        Task<bool> SwitchProfileAsync(string deviceAddress, AudioProfile profile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a specific feature is supported by the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="feature">The feature name to check (e.g., "volume", "browsing", "seek").</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the feature is supported; otherwise, false.</returns>
        /// <example>
        /// <code>
        /// bool canBrowse = await controller.IsFeatureSupportedAsync(deviceAddress, "browsing");
        /// if (canBrowse)
        /// {
        ///     var folders = await controller.BrowseMediaAsync(deviceAddress);
        /// }
        /// </code>
        /// </example>
        Task<bool> IsFeatureSupportedAsync(string deviceAddress, string feature, CancellationToken cancellationToken = default);

        // Audio Processing Control

        /// <summary>
        /// Configures audio processing settings like dynamic range compression for the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="settings">The audio processing settings to apply.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the settings were applied successfully; otherwise, false.</returns>
        /// <remarks>
        /// This method uses PulseAudio LADSPA plugins to apply real-time audio processing.
        /// Ensure LADSPA plugins are installed: sudo apt install ladspa-sdk swh-plugins
        /// </remarks>
        Task<bool> SetDynamicRangeCompressionAsync(string deviceAddress, AudioProcessingSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current audio processing settings for the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The current audio processing settings, or null if none are active.</returns>
        Task<AudioProcessingSettings> GetAudioProcessingSettingsAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to set the audio codec used for the device connection.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="codecName">The name of the codec to use (e.g., "aptX", "AAC", "SBC").</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the codec change was initiated; otherwise, false.</returns>
        /// <remarks>
        /// Codec switching may require reconnection and is not guaranteed to work with all devices.
        /// The actual codec used depends on what both devices support.
        /// </remarks>
        Task<bool> SetAudioCodecAsync(string deviceAddress, string codecName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed information about the audio codecs supported and in use by the device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A2DP capability information including supported codecs.</returns>
        Task<A2dpCapabilities> GetAudioCodecInfoAsync(string deviceAddress, CancellationToken cancellationToken = default);
    }
}