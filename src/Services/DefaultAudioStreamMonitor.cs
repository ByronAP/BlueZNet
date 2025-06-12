using BlueZNet.Events;
using BlueZNet.Interfaces;
using BlueZNet.Interfaces.DBus;
using BlueZNet.Models.Audio;
using BlueZNet.Models.Capabilities;
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
    /// Default implementation of audio stream monitor for monitoring audio streams and managing audio settings.
    /// </summary>
    public class DefaultAudioStreamMonitor : IAudioStreamMonitor, IDisposable
    {
        private const int AudioStreamMonitorIntervalSeconds = 5;

        private readonly ILogger _logger;
        private readonly IPulseAudioService _pulseAudioService;
        private readonly IBlueZDeviceManager _deviceManager;
        private readonly IDBusConnectionFactory _dbusFactory;
        private readonly BlueZNetConfiguration _configuration;

        private Timer _audioStreamTimer;
        private CancellationTokenSource _monitoringCancellationTokenSource;
        private readonly SemaphoreSlim _audioStreamSemaphore = new SemaphoreSlim(1, 1);
        private Connection _connection;
        private bool _isMonitoring;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<AudioStreamEventArgs> AudioStreamChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAudioStreamMonitor"/> class.
        /// </summary>
        /// <param name="pulseAudioService">The PulseAudio service for audio operations.</param>
        /// <param name="deviceManager">The device manager for device information.</param>
        /// <param name="logger">The logger instance.</param>
        public DefaultAudioStreamMonitor(IPulseAudioService pulseAudioService, IBlueZDeviceManager deviceManager, ILogger logger = null)
            : this(pulseAudioService, deviceManager, new DefaultDBusConnectionFactory(logger), logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAudioStreamMonitor"/> class with a custom D-Bus factory.
        /// </summary>
        /// <param name="pulseAudioService">The PulseAudio service for audio operations.</param>
        /// <param name="deviceManager">The device manager for device information.</param>
        /// <param name="dbusFactory">The D-Bus connection factory for BlueZ communication.</param>
        /// <param name="logger">The logger instance.</param>
        public DefaultAudioStreamMonitor(IPulseAudioService pulseAudioService, IBlueZDeviceManager deviceManager, IDBusConnectionFactory dbusFactory, ILogger logger = null)
            : this(pulseAudioService, deviceManager, dbusFactory, logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAudioStreamMonitor"/> class with full dependency injection.
        /// </summary>
        /// <param name="pulseAudioService">The PulseAudio service for audio operations.</param>
        /// <param name="deviceManager">The device manager for device information.</param>
        /// <param name="dbusFactory">The D-Bus connection factory for BlueZ communication.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configuration">The configuration instance.</param>
        public DefaultAudioStreamMonitor(
            IPulseAudioService pulseAudioService,
            IBlueZDeviceManager deviceManager,
            IDBusConnectionFactory dbusFactory,
            ILogger logger,
            BlueZNetConfiguration configuration)
        {
            _pulseAudioService = pulseAudioService ?? throw new ArgumentNullException(nameof(pulseAudioService));
            _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            _dbusFactory = dbusFactory ?? throw new ArgumentNullException(nameof(dbusFactory));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _configuration = configuration ?? BlueZNetConfiguration.Default;
        }

        /// <inheritdoc />
        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (_isMonitoring)
                return;

            try
            {
                _logger.LogDebug("Starting audio stream monitoring");

                _monitoringCancellationTokenSource = new CancellationTokenSource();
                StartAudioStreamMonitoring();

                _isMonitoring = true;
                _logger.LogInformation("Audio stream monitoring started successfully");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start audio stream monitoring");
                await StopMonitoringAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
                return;

            _logger.LogDebug("Stopping audio stream monitoring");

            try
            {
                _monitoringCancellationTokenSource?.Cancel();
                await Task.Delay(100); // Give tasks a chance to cancel gracefully
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during monitoring cancellation");
            }

            StopAudioStreamMonitoring();
            _isMonitoring = false;
            _logger.LogInformation("Audio stream monitoring stopped");
        }

        /// <inheritdoc />
        public async Task<bool> SetVolumeAsync(string deviceAddress, double volume, CancellationToken cancellationToken = default)
        {
            try
            {
                volume = Math.Max(0.0, Math.Min(1.0, volume)); // Clamp between 0.0 and 1.0

                var audioInfo = await _pulseAudioService.GetAudioStreamInfoAsync(deviceAddress, cancellationToken);
                if (audioInfo?.SinkName == null)
                {
                    _logger.LogWarning("No audio sink found for device {DeviceAddress}", deviceAddress);
                    return false;
                }

                var result = await _pulseAudioService.SetVolumeAsync(audioInfo.SinkName, volume, cancellationToken);

                if (result)
                {
                    _logger.LogDebug("Set volume to {Volume:P0} for device {DeviceAddress}", volume, deviceAddress);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set volume for device {DeviceAddress}", deviceAddress);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetMutedAsync(string deviceAddress, bool muted, CancellationToken cancellationToken = default)
        {
            try
            {
                var audioInfo = await _pulseAudioService.GetAudioStreamInfoAsync(deviceAddress, cancellationToken);
                if (audioInfo?.SinkName == null)
                {
                    _logger.LogWarning("No audio sink found for device {DeviceAddress}", deviceAddress);
                    return false;
                }

                var result = await _pulseAudioService.SetMutedAsync(audioInfo.SinkName, muted, cancellationToken);

                if (result)
                {
                    _logger.LogDebug("Set mute {Muted} for device {DeviceAddress}", muted, deviceAddress);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set mute for device {DeviceAddress}", deviceAddress);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<double?> GetVolumeAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                var audioInfo = await _pulseAudioService.GetAudioStreamInfoAsync(deviceAddress, cancellationToken);
                return audioInfo?.Volume;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get volume for device {DeviceAddress}", deviceAddress);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetAudioCodecAsync(string deviceAddress, string codecName, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var connectedDevices = await _deviceManager.GetConnectedDevicesAsync(cancellationToken);
                var device = connectedDevices.FirstOrDefault(d => d.Address == deviceAddress);

                if (device == null)
                {
                    _logger.LogWarning("Device {DeviceAddress} not found", deviceAddress);
                    return false;
                }

                // Get current A2DP capabilities to verify codec is supported
                var capabilities = await GetAudioCodecInfoAsync(deviceAddress, cancellationToken);
                var targetCodec = capabilities?.SupportedCodecs?.FirstOrDefault(c =>
                    c.Name.Equals(codecName, StringComparison.OrdinalIgnoreCase));

                if (targetCodec == null)
                {
                    _logger.LogWarning("Codec {CodecName} not supported by device {DeviceAddress}", codecName, deviceAddress);
                    return false;
                }

                // Get the active media transport for this device
                var activeTransport = await GetActiveMediaTransportAsync(deviceAddress, cancellationToken);
                if (activeTransport == null)
                {
                    _logger.LogWarning("No active media transport found for device {DeviceAddress}", deviceAddress);
                    return false;
                }

                // Check if the desired codec is already active
                if (targetCodec.IsActive)
                {
                    _logger.LogDebug("Codec {CodecName} is already active for device {DeviceAddress}", codecName, deviceAddress);
                    return true;
                }

                _logger.LogInformation("Switching device {DeviceAddress} from codec {CurrentCodec} to {TargetCodec}",
                    deviceAddress, capabilities.ActiveCodec?.Name ?? "Unknown", codecName);

                // Attempt codec switching through BlueZ MediaTransport reconfiguration
                return await ReconfigureMediaTransportCodecAsync(activeTransport, targetCodec, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set audio codec {CodecName} for device {DeviceAddress}", codecName, deviceAddress);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<A2dpCapabilities> GetAudioCodecInfoAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                var capabilities = await _deviceManager.GetDeviceCapabilitiesAsync(deviceAddress, cancellationToken);
                return capabilities?.A2dp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audio codec info for device {DeviceAddress}", deviceAddress);
                return null;
            }
        }

        /// <summary>
        /// Gets the active media transport for the specified device.
        /// </summary>
        private async Task<MediaTransportInfo> GetActiveMediaTransportAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            var transports = await _deviceManager.GetMediaTransportsAsync(deviceAddress, cancellationToken);
            return transports.FirstOrDefault(t => t.State == "active" || t.State == "pending");
        }

        /// <summary>
        /// Attempts to reconfigure the media transport to use a different codec.
        /// </summary>
        private async Task<bool> ReconfigureMediaTransportCodecAsync(MediaTransportInfo transport, AudioCodec targetCodec, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Attempting to reconfigure transport {ObjectPath} to use codec {CodecName}",
                    transport.ObjectPath, targetCodec.Name);

                // This is a complex operation that typically requires:
                // 1. Releasing the current transport
                // 2. Finding/configuring appropriate endpoint for target codec
                // 3. Establishing new transport with target codec

                // For now, we'll try a simpler approach: disconnect and reconnect
                // which may trigger codec renegotiation
                return await TriggerCodecRenegotiationAsync(transport, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconfigure transport {ObjectPath} for codec {CodecName}",
                    transport.ObjectPath, targetCodec.Name);
                return false;
            }
        }

        /// <summary>
        /// Triggers codec renegotiation by cycling the media transport connection.
        /// </summary>
        private async Task<bool> TriggerCodecRenegotiationAsync(MediaTransportInfo transport, CancellationToken cancellationToken)
        {
            try
            {
                await EnsureConnectionAsync(cancellationToken);

                // Create proxy for the media transport
                var transportProxy = _dbusFactory.CreateProxy<IMediaTransport>(_connection, "org.bluez", transport.ObjectPath);

                // Try to release and reacquire the transport
                await transportProxy.ReleaseAsync();
                await Task.Delay(1000, cancellationToken); // Give time for disconnection

                // Reacquire with default configuration - BlueZ will renegotiate codec
                await transportProxy.AcquireAsync();

                _logger.LogDebug("Successfully triggered codec renegotiation for transport {ObjectPath}", transport.ObjectPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to trigger codec renegotiation for transport {ObjectPath}", transport.ObjectPath);
                return false;
            }
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
        /// Starts the audio stream monitoring timer.
        /// </summary>
        private void StartAudioStreamMonitoring()
        {
            _audioStreamTimer = new Timer(
                _ => SafeInvokeAsync(MonitorAudioStreamsAsync),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(_configuration.AudioStreamMonitorIntervalSeconds));
        }

        /// <summary>
        /// Stops the audio stream monitoring timer.
        /// </summary>
        private void StopAudioStreamMonitoring()
        {
            _audioStreamTimer?.Dispose();
            _audioStreamTimer = null;

            _monitoringCancellationTokenSource?.Dispose();
            _monitoringCancellationTokenSource = null;
        }

        /// <summary>
        /// Monitors audio streams for all connected devices.
        /// </summary>
        private async Task MonitorAudioStreamsAsync()
        {
            if (!await _audioStreamSemaphore.WaitAsync(TimeSpan.Zero))
                return; // Skip if already running

            try
            {
                if (_monitoringCancellationTokenSource?.IsCancellationRequested == true)
                    return;

                var connectedDevices = await _deviceManager.GetConnectedDevicesAsync(_monitoringCancellationTokenSource?.Token ?? CancellationToken.None);

                foreach (var device in connectedDevices)
                {
                    if (_monitoringCancellationTokenSource?.IsCancellationRequested == true)
                        break;

                    var audioInfo = await _pulseAudioService.GetAudioStreamInfoAsync(device.Address, _monitoringCancellationTokenSource?.Token ?? CancellationToken.None);

                    // Only fire event if audio info actually changed
                    var existingAudioInfo = device.AudioStream;
                    if (!AudioStreamInfoEquals(audioInfo, existingAudioInfo))
                    {
                        if (audioInfo != null)
                        {
                            var updatedDevice = device.WithUpdatedProperties(audioStream: audioInfo);
                            AudioStreamChanged?.Invoke(this, new AudioStreamEventArgs(updatedDevice, audioInfo));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring audio streams");
            }
            finally
            {
                _audioStreamSemaphore.Release();
            }
        }

        /// <summary>
        /// Compares two AudioStreamInfo objects for equality.
        /// </summary>
        private static bool AudioStreamInfoEquals(AudioStreamInfo a, AudioStreamInfo b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        /// <summary>
        /// Safely invokes an async action with exception handling.
        /// </summary>
        private async void SafeInvokeAsync(Func<Task> asyncAction)
        {
            try
            {
                await asyncAction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in async event handler");
            }
        }

        /// <summary>
        /// Releases all resources used by the DefaultAudioStreamMonitor.
        /// </summary>
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

                    // Dispose D-Bus connection
                    if (_connection != null)
                    {
                        var disposeTask = _dbusFactory.DisposeConnectionAsync(_connection);
                        if (!disposeTask.Wait(TimeSpan.FromSeconds(5)))
                        {
                            _logger.LogWarning("D-Bus connection disposal timed out");
                        }
                        _connection = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during disposal");
                }
                finally
                {
                    _audioStreamSemaphore?.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}