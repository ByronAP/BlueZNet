using BlueZNet.Enums;
using BlueZNet.Events;
using BlueZNet.Interfaces;
using BlueZNet.Models.Audio;
using BlueZNet.Models.Capabilities;
using BlueZNet.Models.Config;
using BlueZNet.Models.Device;
using BlueZNet.Models.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Services
{
    /// <summary>
    /// Provides comprehensive Bluetooth device monitoring and control using BlueZ and PulseAudio.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service supports multiple initialization patterns:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Zero-config: <c>new BlueZNetControllerService()</c></description></item>
    /// <item><description>With logging: <c>new BlueZNetControllerService(logger)</c></description></item>
    /// <item><description>Full DI: <c>new BlueZNetControllerService(logger, dbusFactory, processRunner, ...)</c></description></item>
    /// </list>
    /// <para>
    /// For most use cases, consider using the static factory methods:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>BlueZNetController.CreateDefault()</c> - Zero configuration</description></item>
    /// <item><description><c>BlueZNetController.CreateDefault(logger)</c> - With logging</description></item>
    /// <item><description><c>BlueZNetController.CreateBuilder().WithX().Build()</c> - Custom configuration</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Zero configuration approach
    /// using var controller = new BlueZNetControllerService();
    /// await controller.StartMonitoringAsync();
    /// 
    /// // With custom logger
    /// using var controller = new BlueZNetControllerService(logger);
    /// await controller.StartMonitoringAsync();
    /// 
    /// // Full dependency injection
    /// using var controller = new BlueZNetControllerService(
    ///     logger,
    ///     customDBusFactory,
    ///     customProcessRunner,
    ///     customPulseAudioService,
    ///     customDeviceManager,
    ///     customMediaManager,
    ///     customAudioMonitor);
    /// await controller.StartMonitoringAsync();
    /// </code>
    /// </example>
    public class BlueZNetControllerService : IBlueZNetController
    {
        private readonly ILogger<BlueZNetControllerService> _logger;
        private readonly IDBusConnectionFactory _dbusFactory;
        private readonly IProcessRunner _processRunner;
        private readonly IPulseAudioService _pulseAudioService;
        private readonly IBlueZDeviceManager _deviceManager;
        private readonly IMediaPlayerManager _mediaPlayerManager;
        private readonly IAudioStreamMonitor _audioStreamMonitor;
        private readonly BlueZNetConfiguration _configuration;

        private bool _isMonitoring;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<BluetoothConnectionEventArgs> DeviceConnectionChanged;

        /// <inheritdoc />
        public event EventHandler<MediaPlaybackEventArgs> MediaPlaybackChanged;

        /// <inheritdoc />
        public event EventHandler<AudioStreamEventArgs> AudioStreamChanged;

        /// <inheritdoc />
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Initializes a new instance with zero configuration (uses all defaults and null logger).
        /// Perfect for getting started quickly without any setup.
        /// </summary>
        /// <remarks>
        /// This constructor creates all default implementations internally:
        /// <list type="bullet">
        /// <item><description>NullLogger for no log output</description></item>
        /// <item><description>Default D-Bus connection factory</description></item>
        /// <item><description>Default process runner</description></item>
        /// <item><description>Default PulseAudio service</description></item>
        /// <item><description>Default device manager</description></item>
        /// <item><description>Default media player manager</description></item>
        /// <item><description>Default audio stream monitor</description></item>
        /// </list>
        /// </remarks>
        public BlueZNetControllerService()
            : this(NullLogger<BlueZNetControllerService>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom logger and default implementations for everything else.
        /// Use this when you want logging but don't need to customize other components.
        /// </summary>
        /// <param name="logger">The logger to use for all components, or null to use NullLogger.</param>
        /// <remarks>
        /// This constructor creates default implementations for all services but uses your logger
        /// throughout the entire system. This is the most common initialization pattern.
        /// </remarks>
        public BlueZNetControllerService(ILogger<BlueZNetControllerService> logger)
        {
            _logger = logger ?? NullLogger<BlueZNetControllerService>.Instance;
            _configuration = BlueZNetConfiguration.Default;

            // Create default implementations with the provided logger
            _dbusFactory = new DefaultDBusConnectionFactory(_logger);
            _processRunner = new DefaultProcessRunner(_logger, _configuration);
            _pulseAudioService = new DefaultPulseAudioService(_processRunner, _logger, _configuration);
            _deviceManager = new DefaultBlueZDeviceManager(_dbusFactory, _processRunner, _logger, _configuration);
            _mediaPlayerManager = new DefaultMediaPlayerManager(_dbusFactory, _deviceManager, _logger, _configuration);
            _audioStreamMonitor = new DefaultAudioStreamMonitor(_pulseAudioService, _deviceManager, _dbusFactory, _logger, _configuration);

            WireUpEvents();
        }

        /// <summary>
        /// Initializes a new instance with full dependency injection for maximum control and testability.
        /// Use this constructor when you need to customize specific components or for testing scenarios.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="dbusFactory">The D-Bus connection factory.</param>
        /// <param name="processRunner">The process runner for external commands.</param>
        /// <param name="pulseAudioService">The PulseAudio service.</param>
        /// <param name="deviceManager">The device manager.</param>
        /// <param name="mediaPlayerManager">The media player manager.</param>
        /// <param name="audioStreamMonitor">The audio stream monitor.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
        /// <remarks>
        /// <para>
        /// This constructor is designed for advanced scenarios where you need full control over
        /// the behavior of individual components. Common use cases include:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Unit testing with mocked dependencies</description></item>
        /// <item><description>Custom implementations for specific requirements</description></item>
        /// <item><description>Integration with dependency injection containers</description></item>
        /// <item><description>Performance optimization with specialized implementations</description></item>
        /// </list>
        /// <para>
        /// All dependencies are required and will be validated at construction time.
        /// </para>
        /// </remarks>
        public BlueZNetControllerService(
                    ILogger<BlueZNetControllerService> logger,
                    IDBusConnectionFactory dbusFactory,
                    IProcessRunner processRunner,
                    IPulseAudioService pulseAudioService,
                    IBlueZDeviceManager deviceManager,
                    IMediaPlayerManager mediaPlayerManager,
                    IAudioStreamMonitor audioStreamMonitor,
                    BlueZNetConfiguration configuration = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbusFactory = dbusFactory ?? throw new ArgumentNullException(nameof(dbusFactory));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _pulseAudioService = pulseAudioService ?? throw new ArgumentNullException(nameof(pulseAudioService));
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _mediaPlayerManager = mediaPlayerManager ?? throw new ArgumentNullException(nameof(mediaPlayerManager));
            _audioStreamMonitor = audioStreamMonitor ?? throw new ArgumentNullException(nameof(audioStreamMonitor));
            _configuration = configuration ?? BlueZNetConfiguration.Default;

            WireUpEvents();
        }

        /// <summary>
        /// Wires up events from injected services to public events.
        /// </summary>
        private void WireUpEvents()
        {
            _deviceManager.DeviceConnectionChanged += (sender, args) => DeviceConnectionChanged?.Invoke(this, args);
            _mediaPlayerManager.MediaPlaybackChanged += (sender, args) => MediaPlaybackChanged?.Invoke(this, args);
            _audioStreamMonitor.AudioStreamChanged += (sender, args) => AudioStreamChanged?.Invoke(this, args);
        }

        /// <inheritdoc />
        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Bluetooth monitoring is already running");
                return;
            }

            try
            {
                _logger.LogInformation("Starting Bluetooth monitoring...");

                // Start all monitoring services
                await _deviceManager.StartMonitoringAsync(cancellationToken);
                await _audioStreamMonitor.StartMonitoringAsync(cancellationToken);

                _isMonitoring = true;
                _logger.LogInformation("Bluetooth monitoring started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Bluetooth monitoring");
                await StopMonitoringAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
                return;

            _logger.LogInformation("Stopping Bluetooth monitoring...");

            try
            {
                await _audioStreamMonitor.StopMonitoringAsync();
                await _deviceManager.StopMonitoringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during monitoring shutdown");
            }

            _isMonitoring = false;
            _logger.LogInformation("Bluetooth monitoring stopped");
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<BluetoothDevice>> GetConnectedDevicesAsync(CancellationToken cancellationToken = default)
        {
            return await _deviceManager.GetConnectedDevicesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<BluetoothDevice>> GetKnownDevicesAsync(CancellationToken cancellationToken = default)
        {
            return await _deviceManager.GetKnownDevicesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AudioStreamInfo> GetAudioStreamInfoAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _pulseAudioService.GetAudioStreamInfoAsync(deviceAddress, cancellationToken);
        }

        // Media Control Methods - All delegate to MediaPlayerManager

        /// <inheritdoc />
        public async Task<bool> PlayAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.PlayAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> PauseAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.PauseAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> StopAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.StopAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> NextTrackAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.NextTrackAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> PreviousTrackAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.PreviousTrackAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> FastForwardAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.FastForwardAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> RewindAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.RewindAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SeekAsync(string deviceAddress, uint positionMs, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.SeekAsync(deviceAddress, positionMs, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<uint?> GetPositionAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.GetPositionAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SetVolumeAsync(string deviceAddress, double volume, CancellationToken cancellationToken = default)
        {
            return await _audioStreamMonitor.SetVolumeAsync(deviceAddress, volume, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SetMutedAsync(string deviceAddress, bool muted, CancellationToken cancellationToken = default)
        {
            return await _audioStreamMonitor.SetMutedAsync(deviceAddress, muted, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<double?> GetVolumeAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _audioStreamMonitor.GetVolumeAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SetShuffleAsync(string deviceAddress, bool shuffle, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.SetShuffleAsync(deviceAddress, shuffle, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SetRepeatModeAsync(string deviceAddress, string mode, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.SetRepeatModeAsync(deviceAddress, mode, cancellationToken);
        }

        // Media Browsing Methods - All delegate to MediaPlayerManager

        /// <inheritdoc />
        public async Task<IReadOnlyList<MediaFolder>> BrowseMediaAsync(string deviceAddress, string folderPath = null, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.BrowseMediaAsync(deviceAddress, folderPath, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<MediaItem>> GetFolderItemsAsync(string deviceAddress, string folderPath, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.GetFolderItemsAsync(deviceAddress, folderPath, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> PlayItemAsync(string deviceAddress, string itemPath, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.PlayItemAsync(deviceAddress, itemPath, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> AddToQueueAsync(string deviceAddress, string itemPath, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.AddToQueueAsync(deviceAddress, itemPath, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<PlaylistInfo> GetCurrentPlaylistAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _mediaPlayerManager.GetCurrentPlaylistAsync(deviceAddress, cancellationToken);
        }

        // Device Capabilities & Profile Management - All delegate to DeviceManager

        /// <inheritdoc />
        public async Task<DeviceCapabilities> GetDeviceCapabilitiesAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            // Get base capabilities from the device manager
            var baseCapabilities = await _deviceManager.GetDeviceCapabilitiesAsync(deviceAddress, cancellationToken);
            if (baseCapabilities == null)
            {
                // Device not found or other error, return empty capabilities.
                return new DeviceCapabilities();
            }

            // Get A2DP capabilities from the audio monitor
            var a2dpCapabilities = await _audioStreamMonitor.GetAudioCodecInfoAsync(deviceAddress, cancellationToken);

            // Combine them into a single comprehensive object
            return new DeviceCapabilities(
                baseCapabilities.Avrcp,
                a2dpCapabilities,
                baseCapabilities.SupportedProfiles,
                baseCapabilities.SupportsAbsoluteVolume,
                baseCapabilities.SupportsBrowsing,
                baseCapabilities.SupportsSearch);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AudioProfile>> GetSupportedProfilesAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _deviceManager.GetSupportedProfilesAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AudioProfile?> GetActiveProfileAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _deviceManager.GetActiveProfileAsync(deviceAddress, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SwitchProfileAsync(string deviceAddress, AudioProfile profile, CancellationToken cancellationToken = default)
        {
            return await _deviceManager.SwitchProfileAsync(deviceAddress, profile, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> IsFeatureSupportedAsync(string deviceAddress, string feature, CancellationToken cancellationToken = default)
        {
            return await _deviceManager.IsFeatureSupportedAsync(deviceAddress, feature, cancellationToken);
        }

        // Audio Processing Control - All delegate to PulseAudioService

        /// <inheritdoc />
        public async Task<bool> SetDynamicRangeCompressionAsync(string deviceAddress, AudioProcessingSettings settings, CancellationToken cancellationToken = default)
        {
            var audioInfo = await _pulseAudioService.GetAudioStreamInfoAsync(deviceAddress, cancellationToken);
            if (audioInfo?.SinkName == null)
            {
                _logger.LogWarning("No audio sink found for device {DeviceAddress}", deviceAddress);
                return false;
            }

            return await _pulseAudioService.SetDynamicRangeCompressionAsync(audioInfo.SinkName, settings, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AudioProcessingSettings> GetAudioProcessingSettingsAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            var audioInfo = await _pulseAudioService.GetAudioStreamInfoAsync(deviceAddress, cancellationToken);
            if (audioInfo?.SinkName == null)
                return null;

            return await _pulseAudioService.GetAudioProcessingSettingsAsync(audioInfo.SinkName, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> SetAudioCodecAsync(string deviceAddress, string codecName, CancellationToken cancellationToken = default)
        {
            return await _audioStreamMonitor.SetAudioCodecAsync(deviceAddress, codecName, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<A2dpCapabilities> GetAudioCodecInfoAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await _audioStreamMonitor.GetAudioCodecInfoAsync(deviceAddress, cancellationToken);
        }

        /// <summary>
        /// Releases all resources used by the BlueZNetControllerService.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method will attempt to gracefully stop monitoring and dispose of all
        /// injected services that implement IDisposable. If graceful shutdown takes
        /// longer than 5 seconds, it will timeout and log a warning.
        /// </para>
        /// <para>
        /// Note: Only services created by this instance (not injected) will be disposed.
        /// If you injected custom implementations, you're responsible for disposing them.
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    var stopTask = StopMonitoringAsync();
                    if (!stopTask.Wait(TimeSpan.FromSeconds(5)))
                    {
                        _logger.LogWarning("StopMonitoringAsync timed out during disposal");
                    }

                    // Dispose services if they implement IDisposable
                    // Note: We only dispose services we know about, not injected ones
                    // because the caller is responsible for managing their lifecycle
                    if (_deviceManager is IDisposable deviceManagerDisposable)
                        deviceManagerDisposable.Dispose();

                    if (_mediaPlayerManager is IDisposable mediaManagerDisposable)
                        mediaManagerDisposable.Dispose();

                    if (_audioStreamMonitor is IDisposable audioMonitorDisposable)
                        audioMonitorDisposable.Dispose();
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