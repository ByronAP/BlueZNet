using BlueZNet.Events;
using BlueZNet.Models.Media;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Interfaces
{
    /// <summary>
    /// Manages media player operations and media browsing for connected devices.
    /// Implement this interface to customize media control behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts media player control, allowing you to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Customize media command execution and validation</description></item>
    /// <item><description>Implement alternative media browsing strategies</description></item>
    /// <item><description>Add media operation caching and optimization</description></item>
    /// <item><description>Mock media operations for testing scenarios</description></item>
    /// <item><description>Integrate with different media control protocols</description></item>
    /// </list>
    /// </remarks>
    public interface IMediaPlayerManager
    {
        /// <summary>
        /// Starts or resumes playback on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the command was sent successfully; otherwise, false.</returns>
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

        /// <summary>
        /// Seeks to a specific position in the current track.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="positionMs">The position to seek to, in milliseconds.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the seek was successful; otherwise, false.</returns>
        Task<bool> SeekAsync(string deviceAddress, uint positionMs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current playback position of the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The current position in milliseconds, or null if unavailable.</returns>
        Task<uint?> GetPositionAsync(string deviceAddress, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// Browses the media folders available on the specified device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the device.</param>
        /// <param name="folderPath">The path to browse, or null for top-level folders.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A list of available media folders.</returns>
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

        /// <summary>
        /// Occurs when a device's media playback state changes.
        /// </summary>
        event EventHandler<MediaPlaybackEventArgs> MediaPlaybackChanged;
    }
}