using BlueZNet.Events;
using BlueZNet.Interfaces;
using BlueZNet.Interfaces.DBus;
using BlueZNet.Models.Config;
using BlueZNet.Models.Media;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Services
{
    /// <summary>
    /// Default implementation of media player manager for media control operations.
    /// </summary>
    public class DefaultMediaPlayerManager : IMediaPlayerManager, IDisposable
    {
        private const string BluezService = "org.bluez";
        private const string MediaPlayerInterface = "org.bluez.MediaPlayer1";
        private const string MediaFolderInterface = "org.bluez.MediaFolder1";
        private const string MediaItemInterface = "org.bluez.MediaItem1";

        private readonly ILogger _logger;
        private readonly IDBusConnectionFactory _dbusFactory;
        private readonly IBlueZDeviceManager _deviceManager;
        private readonly BlueZNetConfiguration _configuration;

        private Connection _connection;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<MediaPlaybackEventArgs> MediaPlaybackChanged;

        /// <summary>
        /// Initializes a new instance with default configuration.
        /// </summary>
        public DefaultMediaPlayerManager(IDBusConnectionFactory dbusFactory, IBlueZDeviceManager deviceManager, ILogger logger = null)
            : this(dbusFactory, deviceManager, logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultMediaPlayerManager"/> class.
        /// </summary>
        /// <param name="dbusFactory">The D-Bus connection factory.</param>
        /// <param name="deviceManager">The device manager for device information.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configuration">The configuration instance.</param>
        public DefaultMediaPlayerManager(IDBusConnectionFactory dbusFactory, IBlueZDeviceManager deviceManager, ILogger logger, BlueZNetConfiguration configuration)
        {
            _dbusFactory = dbusFactory ?? throw new ArgumentNullException(nameof(dbusFactory));
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _configuration = configuration ?? BlueZNetConfiguration.Default;
        }

        /// <inheritdoc />
        public async Task<bool> PlayAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player => await player.PlayAsync(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> PauseAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player => await player.PauseAsync(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> StopAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player => await player.StopAsync(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> NextTrackAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player => await player.NextAsync(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> PreviousTrackAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player => await player.PreviousAsync(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> FastForwardAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player => await player.FastForwardAsync(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> RewindAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player => await player.RewindAsync(), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SeekAsync(string deviceAddress, uint positionMs, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player =>
            {
                await player.SetAsync("Position", positionMs);
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<uint?> GetPositionAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mediaPlayerPath = await GetMediaPlayerPathAsync(deviceAddress, cancellationToken);
                if (mediaPlayerPath == null) return null;

                await EnsureConnectionAsync(cancellationToken);

                var player = _dbusFactory.CreateProxy<IMediaPlayer>(_connection, BluezService, mediaPlayerPath);
                var position = await player.GetAsync<uint>("Position");
                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get position for device {DeviceAddress}", deviceAddress);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetShuffleAsync(string deviceAddress, bool shuffle, CancellationToken cancellationToken = default)
        {
            return await ExecuteMediaCommandAsync(deviceAddress, async player =>
            {
                await player.SetAsync("Shuffle", shuffle);
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SetRepeatModeAsync(string deviceAddress, string mode, CancellationToken cancellationToken = default)
        {
            var validModes = new[] { "off", "singletrack", "alltracks", "group" };
            if (!validModes.Contains(mode.ToLower()))
            {
                _logger.LogWarning("Invalid repeat mode: {Mode}. Valid modes: {ValidModes}", mode, string.Join(", ", validModes));
                return false;
            }

            return await ExecuteMediaCommandAsync(deviceAddress, async player =>
            {
                await player.SetAsync("Repeat", mode.ToLower());
            }, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<MediaFolder>> BrowseMediaAsync(string deviceAddress, string folderPath = null, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var connectedDevices = await _deviceManager.GetConnectedDevicesAsync(cancellationToken);
                var device = connectedDevices.FirstOrDefault(d => d.Address == deviceAddress);

                if (device?.MediaPlayer == null)
                {
                    _logger.LogWarning("No media player found for device {DeviceAddress}", deviceAddress);
                    return new List<MediaFolder>();
                }

                await EnsureConnectionAsync(cancellationToken);

                // Get the device's object path for filtering folders
                var deviceObjectPath = device.ObjectPath;
                var mediaPlayerObjectPath = device.MediaPlayer.ObjectPath;

                // Get all managed objects from BlueZ
                var objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, BluezService, "/");
                var managedObjects = await objectManager.GetManagedObjectsAsync();

                var folders = new List<MediaFolder>();

                foreach (var kvp in managedObjects)
                {
                    var objectPath = kvp.Key.ToString();
                    var interfaces = kvp.Value;

                    // Check if this object has the MediaFolder interface
                    if (interfaces.ContainsKey(MediaFolderInterface))
                    {
                        // Verify this folder belongs to our device
                        if (IsMediaFolderForDevice(objectPath, deviceObjectPath, mediaPlayerObjectPath))
                        {
                            // If folderPath is specified, only include folders under that path
                            if (folderPath != null)
                            {
                                if (!IsSubFolderOf(objectPath, folderPath))
                                    continue;

                                // Only include direct children, not deep descendants
                                if (!IsDirectChild(objectPath, folderPath))
                                    continue;
                            }
                            else
                            {
                                // If no specific folder path, only include top-level folders
                                if (!IsTopLevelMediaFolder(objectPath, mediaPlayerObjectPath))
                                    continue;
                            }

                            var folderProperties = interfaces[MediaFolderInterface];
                            var folder = await CreateMediaFolderAsync(objectPath, folderProperties, cancellationToken);

                            if (folder != null)
                            {
                                folders.Add(folder);
                                _logger.LogTrace("Found media folder: {Name} at {Path}", folder.Name, objectPath);
                            }
                        }
                    }
                }

                // Sort folders by name for consistent ordering
                folders.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

                _logger.LogDebug("Found {Count} media folders for device {DeviceAddress} in path {FolderPath}",
                    folders.Count, deviceAddress, folderPath ?? "root");

                return folders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to browse media for device {DeviceAddress} in path {FolderPath}",
                    deviceAddress, folderPath);
                return new List<MediaFolder>();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<MediaItem>> GetFolderItemsAsync(string deviceAddress, string folderPath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await EnsureConnectionAsync(cancellationToken);

                var folder = _dbusFactory.CreateProxy<IMediaFolder>(_connection, BluezService, folderPath);
                var result = await folder.ListItemsAsync();
                var itemPaths = result.items;
                var itemProperties = result.properties;

                var items = new List<MediaItem>();
                for (int i = 0; i < itemPaths.Length && i < itemProperties.Length; i++)
                {
                    var item = CreateMediaItem(itemPaths[i].ToString(), itemProperties[i]);
                    if (item != null)
                    {
                        items.Add(item);
                    }
                }

                _logger.LogDebug("Found {Count} items in folder {FolderPath}", items.Count, folderPath);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get folder items for {FolderPath}", folderPath);
                return new List<MediaItem>();
            }
        }

        /// <inheritdoc />
        public async Task<bool> PlayItemAsync(string deviceAddress, string itemPath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await EnsureConnectionAsync(cancellationToken);

                var item = _dbusFactory.CreateProxy<IMediaItem>(_connection, BluezService, itemPath);
                await item.PlayAsync();

                _logger.LogDebug("Started playing item {ItemPath} on device {DeviceAddress}", itemPath, deviceAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play item {ItemPath} on device {DeviceAddress}", itemPath, deviceAddress);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> AddToQueueAsync(string deviceAddress, string itemPath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                await EnsureConnectionAsync(cancellationToken);

                var item = _dbusFactory.CreateProxy<IMediaItem>(_connection, BluezService, itemPath);
                await item.AddtoNowPlayingAsync();

                _logger.LogDebug("Added item {ItemPath} to queue on device {DeviceAddress}", itemPath, deviceAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add item {ItemPath} to queue on device {DeviceAddress}", itemPath, deviceAddress);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<PlaylistInfo> GetCurrentPlaylistAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mediaPlayerPath = await GetMediaPlayerPathAsync(deviceAddress, cancellationToken);
                if (mediaPlayerPath == null) return null;

                await EnsureConnectionAsync(cancellationToken);

                var player = _dbusFactory.CreateProxy<IMediaPlayer>(_connection, BluezService, mediaPlayerPath);
                var properties = await player.GetAllAsync();

                // Try to extract playlist information from available properties
                string playlistName = null;
                uint? totalTracks = null;
                uint? currentIndex = null;

                if (properties.TryGetValue("Playlist", out var playlistObj) && playlistObj is IDictionary<string, object> playlistDict)
                {
                    if (playlistDict.TryGetValue("Name", out var nameObj) && nameObj is string name)
                        playlistName = name;

                    if (playlistDict.TryGetValue("TotalTracks", out var totalObj) && totalObj is uint total)
                        totalTracks = total;

                    if (playlistDict.TryGetValue("CurrentIndex", out var indexObj) && indexObj is uint index)
                        currentIndex = index;
                }

                if (playlistName != null || totalTracks.HasValue)
                {
                    return new PlaylistInfo(playlistName, totalTracks, currentIndex);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current playlist for device {DeviceAddress}", deviceAddress);
                return null;
            }
        }

        /// <summary>
        /// Executes a media command on the specified device's media player.
        /// </summary>
        private async Task<bool> ExecuteMediaCommandAsync(string deviceAddress, Func<IMediaPlayer, Task> command, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mediaPlayerPath = await GetMediaPlayerPathAsync(deviceAddress, cancellationToken);
                if (mediaPlayerPath == null)
                {
                    _logger.LogWarning("No media player found for device {DeviceAddress}", deviceAddress);
                    return false;
                }

                await EnsureConnectionAsync(cancellationToken);

                var player = _dbusFactory.CreateProxy<IMediaPlayer>(_connection, BluezService, mediaPlayerPath);
                await command(player);

                _logger.LogDebug("Media command executed successfully for device {DeviceAddress}", deviceAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute media command for device {DeviceAddress}", deviceAddress);
                return false;
            }
        }

        /// <summary>
        /// Gets the D-Bus object path for a device's media player.
        /// </summary>
        private async Task<string> GetMediaPlayerPathAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            var connectedDevices = await _deviceManager.GetConnectedDevicesAsync(cancellationToken);
            var device = connectedDevices.FirstOrDefault(d => d.Address == deviceAddress);

            return device?.MediaPlayer?.ObjectPath;
        }

        /// <summary>
        /// Ensures a D-Bus connection is available.
        /// </summary>
        private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
        {
            if (_connection == null)
            {
                _connection = await _dbusFactory.CreateSystemConnectionAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Creates a MediaItem instance from BlueZ item properties.
        /// </summary>
        private MediaItem CreateMediaItem(string objectPath, IDictionary<string, object> properties)
        {
            try
            {
                if (!properties.TryGetValue("Name", out var nameObj) || !(nameObj is string name))
                    return null;

                var type = properties.TryGetValue("Type", out var typeObj) && typeObj is string typeStr ? typeStr : "audio";
                var playable = properties.TryGetValue("Playable", out var playableObj) && playableObj is bool playableBool && playableBool;

                TrackMetadata metadata = null;
                if (properties.TryGetValue("Metadata", out var metadataObj) && metadataObj is IDictionary<string, object> metadataDict)
                {
                    metadata = CreateTrackMetadata(metadataDict);
                }

                return new MediaItem(objectPath, name, type, playable, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create MediaItem from properties");
                return null;
            }
        }

        /// <summary>
        /// Creates a TrackMetadata instance from BlueZ track properties.
        /// </summary>
        private TrackMetadata CreateTrackMetadata(IDictionary<string, object> trackDict)
        {
            var title = trackDict.TryGetValue("Title", out var titleObj) && titleObj is string titleStr ? titleStr : null;
            var artist = trackDict.TryGetValue("Artist", out var artistObj) && artistObj is string artistStr ? artistStr : null;
            var album = trackDict.TryGetValue("Album", out var albumObj) && albumObj is string albumStr ? albumStr : null;
            var genre = trackDict.TryGetValue("Genre", out var genreObj) && genreObj is string genreStr ? genreStr : null;
            var numberOfTracks = trackDict.TryGetValue("NumberOfTracks", out var numTracksObj) && numTracksObj is uint numTracksVal ? (uint?)numTracksVal : null;
            var trackNumber = trackDict.TryGetValue("TrackNumber", out var trackNumObj) && trackNumObj is uint trackNumVal ? (uint?)trackNumVal : null;
            var duration = trackDict.TryGetValue("Duration", out var durationObj) && durationObj is uint durationVal ? (uint?)durationVal : null;

            return new TrackMetadata(title, artist, album, genre, numberOfTracks, trackNumber, duration);
        }

        /// <summary>
        /// Determines if a media folder object path belongs to the specified device.
        /// </summary>
        private bool IsMediaFolderForDevice(string folderPath, string deviceObjectPath, string mediaPlayerObjectPath)
        {
            // Media folder paths typically follow patterns like:
            // /org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF/player0/Filesystem
            // /org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF/player0/NowPlaying
            // So they should start with the media player path

            return folderPath.StartsWith(mediaPlayerObjectPath + "/", StringComparison.OrdinalIgnoreCase) ||
                   folderPath.StartsWith(deviceObjectPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if a folder is a top-level media folder (direct child of media player).
        /// </summary>
        private bool IsTopLevelMediaFolder(string folderPath, string mediaPlayerObjectPath)
        {
            if (!folderPath.StartsWith(mediaPlayerObjectPath + "/", StringComparison.OrdinalIgnoreCase))
                return false;

            // Count path segments after the media player path
            var relativePath = folderPath.Substring(mediaPlayerObjectPath.Length + 1);
            var segments = relativePath.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            // Top-level folders have exactly one segment after the media player path
            return segments.Length == 1;
        }

        /// <summary>
        /// Determines if a folder is a subfolder of the specified parent folder path.
        /// </summary>
        private bool IsSubFolderOf(string folderPath, string parentFolderPath)
        {
            return folderPath.StartsWith(parentFolderPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if a folder is a direct child of the specified parent folder path.
        /// </summary>
        private bool IsDirectChild(string folderPath, string parentFolderPath)
        {
            if (!IsSubFolderOf(folderPath, parentFolderPath))
                return false;

            // Count path segments after the parent folder path
            var relativePath = folderPath.Substring(parentFolderPath.Length + 1);
            var segments = relativePath.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            // Direct children have exactly one segment after the parent path
            return segments.Length == 1;
        }

        /// <summary>
        /// Creates a MediaFolder instance from BlueZ D-Bus properties with enhanced property extraction.
        /// </summary>
        private async Task<MediaFolder> CreateMediaFolderAsync(string objectPath, IDictionary<string, object> properties, CancellationToken cancellationToken)
        {
            try
            {
                // Get basic properties
                if (!properties.TryGetValue("Name", out var nameObj) || !(nameObj is string name))
                {
                    _logger.LogWarning("MediaFolder at {ObjectPath} has no Name property", objectPath);
                    return null;
                }

                var type = properties.TryGetValue("Type", out var typeObj) && typeObj is string typeStr ? typeStr : "mixed";
                var playable = properties.TryGetValue("Playable", out var playableObj) && playableObj is bool playableBool && playableBool;

                // Try to get NumberOfItems - might require calling GetNumberOfItems method
                uint? numberOfItems = null;
                if (properties.TryGetValue("NumberOfItems", out var numItemsObj) && numItemsObj is uint numItems)
                {
                    numberOfItems = numItems;
                }
                else
                {
                    // Some folders don't expose NumberOfItems as a property, try calling the method
                    try
                    {
                        var folderProxy = _dbusFactory.CreateProxy<IMediaFolder>(_connection, BluezService, objectPath);
                        var itemCount = await folderProxy.GetNumberOfItemsAsync();
                        numberOfItems = itemCount;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogTrace(ex, "Could not get NumberOfItems for folder {ObjectPath}", objectPath);
                        // This is not critical, continue without item count
                    }
                }

                var folder = new MediaFolder(objectPath, name, type, playable, numberOfItems);

                _logger.LogTrace("Created MediaFolder: Name={Name}, Type={Type}, Playable={Playable}, Items={Items}",
                    name, type, playable, numberOfItems);

                return folder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create MediaFolder from properties for path {ObjectPath}", objectPath);
                return null;
            }
        }

        /// <summary>
        /// Gets the folder hierarchy depth relative to the media player root.
        /// </summary>
        private int GetFolderDepth(string folderPath, string mediaPlayerObjectPath)
        {
            if (!folderPath.StartsWith(mediaPlayerObjectPath + "/", StringComparison.OrdinalIgnoreCase))
                return -1;

            var relativePath = folderPath.Substring(mediaPlayerObjectPath.Length + 1);
            var segments = relativePath.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToArray();

            return segments.Length;
        }

        /// <summary>
        /// Validates that a folder path is well-formed and safe to access.
        /// </summary>
        private bool IsValidFolderPath(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return false;

            // Check for path traversal attempts
            if (folderPath.Contains("..") || folderPath.Contains("//"))
                return false;

            // Must start with /org/bluez/
            if (!folderPath.StartsWith("/org/bluez/", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Releases all resources used by the DefaultMediaPlayerManager.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (_connection != null)
                    {
                        var disposeTask = _dbusFactory.DisposeConnectionAsync(_connection);
                        if (!disposeTask.Wait(TimeSpan.FromSeconds(5)))
                        {
                            _logger.LogWarning("Connection disposal timed out");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during disposal");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}
