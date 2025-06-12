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
        /// Constructor with D-Bus factory.
        /// </summary>
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
                cancellationToken.ThrowIfCancellationRequested();

                var connectedDevices = await _deviceManager.GetConnectedDevicesAsync(cancellationToken);
                var device = connectedDevices.FirstOrDefault(d => d.Address == deviceAddress);

                if (device == null)
                {
                    _logger.LogDebug("Device {DeviceAddress} not found", deviceAddress);
                    return null;
                }

                // Ensure we have a D-Bus connection
                await EnsureConnectionAsync(cancellationToken);

                // Get all media transports and endpoints for this device
                var mediaTransports = await GetMediaTransportsForDeviceAsync(deviceAddress, cancellationToken);
                var mediaEndpoints = await GetMediaEndpointsForDeviceAsync(deviceAddress, cancellationToken);

                var supportedCodecs = new List<AudioCodec>();
                AudioCodec activeCodec = null;
                uint maxBitrate = 0;

                // Parse supported codecs from media endpoints
                foreach (var endpoint in mediaEndpoints)
                {
                    var codecs = await ParseCodecsFromEndpointAsync(endpoint, cancellationToken);
                    supportedCodecs.AddRange(codecs);
                }

                // Determine active codec and bitrate from active transport
                var activeTransport = mediaTransports.FirstOrDefault(t => t.State == "active");
                if (activeTransport != null)
                {
                    activeCodec = await GetCodecFromTransportAsync(activeTransport, cancellationToken);
                    if (activeCodec != null)
                    {
                        // Mark the active codec and update supported codecs list
                        for (int i = 0; i < supportedCodecs.Count; i++)
                        {
                            if (supportedCodecs[i].Name.Equals(activeCodec.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                supportedCodecs[i] = new AudioCodec(activeCodec.Name, activeCodec.Bitrate, activeCodec.Quality, true);
                                activeCodec = supportedCodecs[i];
                                maxBitrate = Math.Max(maxBitrate, activeCodec.Bitrate);
                                break;
                            }
                        }
                    }
                }

                // Calculate max bitrate across all codecs
                foreach (var codec in supportedCodecs)
                {
                    maxBitrate = Math.Max(maxBitrate, codec.Bitrate);
                }

                // Determine if high-quality codecs are supported
                bool supportsHighQuality = supportedCodecs.Any(c =>
                    c.Quality == "High" || c.Quality == "Lossless" ||
                    c.Name.Equals("aptX", StringComparison.OrdinalIgnoreCase) ||
                    c.Name.Equals("LDAC", StringComparison.OrdinalIgnoreCase) ||
                    c.Name.Equals("aptX HD", StringComparison.OrdinalIgnoreCase));

                _logger.LogDebug("Found {Count} supported codecs for device {DeviceAddress}, active: {ActiveCodec}",
                    supportedCodecs.Count, deviceAddress, activeCodec?.Name ?? "None");

                return new A2dpCapabilities(supportedCodecs, activeCodec, maxBitrate, supportsHighQuality);
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
            var transports = await GetMediaTransportsForDeviceAsync(deviceAddress, cancellationToken);
            return transports.FirstOrDefault(t => t.State == "active" || t.State == "pending");
        }

        /// <summary>
        /// Gets all media transports associated with the specified device.
        /// </summary>
        private async Task<List<MediaTransportInfo>> GetMediaTransportsForDeviceAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            try
            {
                await EnsureConnectionAsync(cancellationToken);

                var objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, "org.bluez", "/");
                var managedObjects = await objectManager.GetManagedObjectsAsync();

                var transports = new List<MediaTransportInfo>();
                var deviceMacForPath = deviceAddress.Replace(":", "_");

                foreach (var kvp in managedObjects)
                {
                    var objectPath = kvp.Key.ToString();
                    var interfaces = kvp.Value;

                    // Look for MediaTransport objects belonging to our device
                    if (interfaces.ContainsKey("org.bluez.MediaTransport1") &&
                        objectPath.Contains(deviceMacForPath))
                    {
                        var properties = interfaces["org.bluez.MediaTransport1"];
                        var transport = ParseMediaTransportInfo(objectPath, properties);
                        if (transport != null)
                        {
                            transports.Add(transport);
                        }
                    }
                }

                return transports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get media transports for device {DeviceAddress}", deviceAddress);
                return new List<MediaTransportInfo>();
            }
        }

        /// <summary>
        /// Gets all media endpoints associated with the specified device.
        /// </summary>
        private async Task<List<MediaEndpointInfo>> GetMediaEndpointsForDeviceAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            try
            {
                await EnsureConnectionAsync(cancellationToken);

                var objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, "org.bluez", "/");
                var managedObjects = await objectManager.GetManagedObjectsAsync();

                var endpoints = new List<MediaEndpointInfo>();
                var deviceMacForPath = deviceAddress.Replace(":", "_");

                foreach (var kvp in managedObjects)
                {
                    var objectPath = kvp.Key.ToString();
                    var interfaces = kvp.Value;

                    // Look for MediaEndpoint objects belonging to our device
                    if (interfaces.ContainsKey("org.bluez.MediaEndpoint1") &&
                        objectPath.Contains(deviceMacForPath))
                    {
                        var properties = interfaces["org.bluez.MediaEndpoint1"];
                        var endpoint = ParseMediaEndpointInfo(objectPath, properties);
                        if (endpoint != null)
                        {
                            endpoints.Add(endpoint);
                        }
                    }
                }

                return endpoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get media endpoints for device {DeviceAddress}", deviceAddress);
                return new List<MediaEndpointInfo>();
            }
        }

        /// <summary>
        /// Parses media transport information from D-Bus properties.
        /// </summary>
        private MediaTransportInfo ParseMediaTransportInfo(string objectPath, IDictionary<string, object> properties)
        {
            try
            {
                var device = properties.TryGetValue("Device", out var deviceObj) ? deviceObj?.ToString() : "";
                var uuid = properties.TryGetValue("UUID", out var uuidObj) ? uuidObj?.ToString() : "";
                var codec = properties.TryGetValue("Codec", out var codecObj) && codecObj is byte codecByte ? codecByte : (byte)0;
                var state = properties.TryGetValue("State", out var stateObj) ? stateObj?.ToString() : "idle";
                var volume = properties.TryGetValue("Volume", out var volumeObj) && volumeObj is ushort vol ? vol : (ushort)0;

                // Parse configuration if available
                byte[] configuration = null;
                if (properties.TryGetValue("Configuration", out var configObj) && configObj is byte[] configBytes)
                {
                    configuration = configBytes;
                }

                return new MediaTransportInfo(objectPath, device, uuid, codec, state, volume, configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MediaTransport info for {ObjectPath}", objectPath);
                return null;
            }
        }

        /// <summary>
        /// Parses media endpoint information from D-Bus properties.
        /// </summary>
        private MediaEndpointInfo ParseMediaEndpointInfo(string objectPath, IDictionary<string, object> properties)
        {
            try
            {
                var uuid = properties.TryGetValue("UUID", out var uuidObj) ? uuidObj?.ToString() : "";
                var codec = properties.TryGetValue("Codec", out var codecObj) && codecObj is byte codecByte ? codecByte : (byte)0;

                // Parse capabilities if available
                byte[] capabilities = null;
                if (properties.TryGetValue("Capabilities", out var capObj) && capObj is byte[] capBytes)
                {
                    capabilities = capBytes;
                }

                return new MediaEndpointInfo(objectPath, uuid, codec, capabilities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MediaEndpoint info for {ObjectPath}", objectPath);
                return null;
            }
        }

        /// <summary>
        /// Parses supported codecs from a media endpoint.
        /// </summary>
        private async Task<List<AudioCodec>> ParseCodecsFromEndpointAsync(MediaEndpointInfo endpoint, CancellationToken cancellationToken)
        {
            var codecs = new List<AudioCodec>();

            try
            {
                // Parse codec based on the codec ID and capabilities
                var codecInfo = ParseA2dpCodecInfo(endpoint.Codec, endpoint.Capabilities);
                if (codecInfo != null)
                {
                    codecs.Add(codecInfo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse codec from endpoint {ObjectPath}", endpoint.ObjectPath);
            }

            return codecs;
        }

        /// <summary>
        /// Gets codec information from an active media transport.
        /// </summary>
        private async Task<AudioCodec> GetCodecFromTransportAsync(MediaTransportInfo transport, CancellationToken cancellationToken)
        {
            try
            {
                // Parse the active codec from transport configuration
                return ParseA2dpCodecInfo(transport.Codec, transport.Configuration);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get codec from transport {ObjectPath}", transport.ObjectPath);
                return null;
            }
        }

        /// <summary>
        /// Parses A2DP codec information from codec ID and configuration bytes.
        /// </summary>
        private AudioCodec ParseA2dpCodecInfo(byte codecId, byte[] configuration)
        {
            try
            {
                switch (codecId)
                {
                    case 0x00: // SBC
                        return ParseSbcCodec(configuration);
                    case 0x01: // MPEG-1,2 Audio
                        return ParseMpegAudioCodec(configuration);
                    case 0x02: // MPEG-2,4 AAC
                        return ParseAacCodec(configuration);
                    case 0x40: // aptX (vendor specific)
                        return ParseAptxCodec(configuration);
                    case 0x41: // aptX HD (vendor specific)
                        return ParseAptxHdCodec(configuration);
                    case 0xAA: // LDAC (vendor specific)
                        return ParseLdacCodec(configuration);
                    default:
                        return new AudioCodec($"Unknown (0x{codecId:X2})", 0, "Unknown", false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse A2DP codec info for codec ID 0x{CodecId:X2}", codecId);
                return new AudioCodec($"Unknown (0x{codecId:X2})", 0, "Unknown", false);
            }
        }

        /// <summary>
        /// Parses SBC codec configuration.
        /// </summary>
        private AudioCodec ParseSbcCodec(byte[] config)
        {
            if (config == null || config.Length < 4)
                return new AudioCodec("SBC", 328, "Standard", false);

            // SBC configuration parsing
            var samplingFreq = (config[0] & 0xF0) >> 4;
            var channelMode = (config[0] & 0x0F);
            var blockLength = (config[1] & 0xF0) >> 4;
            var subbands = (config[1] & 0x0C) >> 2;
            var allocationMethod = (config[1] & 0x03);
            var minBitpool = config[2];
            var maxBitpool = config[3];

            // Calculate approximate bitrate
            uint bitrate = CalculateSbcBitrate(samplingFreq, channelMode, blockLength, subbands, maxBitpool);

            return new AudioCodec("SBC", bitrate, "Standard", false);
        }

        /// <summary>
        /// Parses AAC codec configuration.
        /// </summary>
        private AudioCodec ParseAacCodec(byte[] config)
        {
            if (config == null || config.Length < 6)
                return new AudioCodec("AAC", 320, "High", false);

            // AAC configuration parsing - simplified
            var objectType = (config[0] & 0xF8) >> 3;
            var samplingFreq = ((config[0] & 0x07) << 1) | ((config[1] & 0x80) >> 7);
            var channels = (config[1] & 0x78) >> 3;
            var vbr = (config[1] & 0x04) != 0;

            // Estimate bitrate based on configuration
            uint bitrate = objectType == 2 ? 320u : 256u; // AAC-LC vs AAC-Main

            return new AudioCodec("AAC", bitrate, "High", false);
        }

        /// <summary>
        /// Parses aptX codec configuration.
        /// </summary>
        private AudioCodec ParseAptxCodec(byte[] config)
        {
            return new AudioCodec("aptX", 352, "High", false);
        }

        /// <summary>
        /// Parses aptX HD codec configuration.
        /// </summary>
        private AudioCodec ParseAptxHdCodec(byte[] config)
        {
            return new AudioCodec("aptX HD", 576, "Lossless", false);
        }

        /// <summary>
        /// Parses LDAC codec configuration.
        /// </summary>
        private AudioCodec ParseLdacCodec(byte[] config)
        {
            if (config == null || config.Length < 4)
                return new AudioCodec("LDAC", 990, "Lossless", false);

            // LDAC supports different quality modes
            var qualityMode = config.Length > 6 ? config[6] & 0x07 : 0;

            uint bitrate = 990;            
            switch(qualityMode)
            {
                case 0:
                    bitrate = 330; // Low quality
                    break;
                case 1:
                    bitrate = 660; // Standard quality
                    break;
                case 2:
                default:
                    bitrate = 990; // High quality
                    break;
            };

            return new AudioCodec("LDAC", bitrate, "Lossless", false);
        }

        /// <summary>
        /// Parses MPEG audio codec configuration.
        /// </summary>
        private AudioCodec ParseMpegAudioCodec(byte[] config)
        {
            return new AudioCodec("MP3", 320, "Standard", false);
        }

        /// <summary>
        /// Calculates SBC bitrate from configuration parameters.
        /// </summary>
        private uint CalculateSbcBitrate(int samplingFreq, int channelMode, int blockLength, int subbands, int bitpool)
        {
            // Simplified SBC bitrate calculation
            var sampleRate = 44100;
            
            switch(samplingFreq)
            {
                case 0x8:
                    sampleRate = 16000;
                    break;
                case 0x4:
                    sampleRate = 32000;
                    break;
                case 0x2:
                default:
                    sampleRate = 44100;
                    break;
                case 0x1:
                    sampleRate = 48000;
                    break;
            };

            var blocks = 16;
            switch(blockLength)
            {
                case 0x8:
                    blocks = 4;
                    break;
                case 0x4:
                    blocks = 8;
                    break;
                case 0x2:
                    blocks = 12;
                    break;
                case 0x1:
                default:
                    blocks = 16;
                    break;
            };

            var bands = subbands == 0x1 ? 8 : 4;
            var channels = channelMode == 0x8 ? 1 : 2; // Mono vs others

            // Simplified calculation
            var bitrate = (sampleRate * bitpool * channels) / (blocks * bands);
            return (uint)Math.Max(bitrate / 1000, 128); // Convert to kbps, minimum 128
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
            if (!await _audioStreamSemaphore.WaitAsync(0))
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