using BlueZNet.Enums;
using BlueZNet.Events;
using BlueZNet.Interfaces;
using BlueZNet.Interfaces.DBus;
using BlueZNet.Models.Audio;
using BlueZNet.Models.Capabilities;
using BlueZNet.Models.Config;
using BlueZNet.Models.Device;
using BlueZNet.Models.Media;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Services
{
    /// <summary>
    /// Default implementation of BlueZ device manager for device discovery and management.
    /// </summary>
    public class DefaultBlueZDeviceManager : IBlueZDeviceManager, IDisposable
    {
        private const string BluezService = "org.bluez";
        private const string BluezPath = "/";
        private const string DeviceInterface = "org.bluez.Device1";

        private readonly ILogger _logger;
        private readonly IDBusConnectionFactory _dbusFactory;
        private readonly IProcessRunner _processRunner;
        private readonly BlueZNetConfiguration _configuration;
        private readonly ConcurrentDictionary<string, BluetoothDevice> _knownDevices = new ConcurrentDictionary<string, BluetoothDevice>();
        private readonly ConcurrentDictionary<string, BluetoothDevice> _connectedDevices = new ConcurrentDictionary<string, BluetoothDevice>();
        private readonly ConcurrentDictionary<string, string> _deviceAddressToObjectPath = new ConcurrentDictionary<string, string>();
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        private Connection _connection;
        private IObjectManager _objectManager;
        private bool _isMonitoring;
        private bool _disposed;

        /// <inheritdoc />
        public event EventHandler<BluetoothConnectionEventArgs> DeviceConnectionChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBlueZDeviceManager"/> class.
        /// </summary>
        /// <param name="dbusFactory">The D-Bus connection factory.</param>
        /// <param name="logger">The logger instance.</param>
        public DefaultBlueZDeviceManager(IDBusConnectionFactory dbusFactory, ILogger logger = null)
            : this(dbusFactory, new DefaultProcessRunner(logger), logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom process runner.
        /// </summary>
        /// <param name="dbusFactory">The D-Bus connection factory.</param>
        /// <param name="processRunner">The process runner for external commands.</param>
        /// <param name="logger">The logger instance.</param>
        public DefaultBlueZDeviceManager(IDBusConnectionFactory dbusFactory, IProcessRunner processRunner, ILogger logger = null)
            : this(dbusFactory, processRunner, logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultBlueZDeviceManager"/> class with full configuration.
        /// </summary>
        /// <param name="dbusFactory">The D-Bus connection factory.</param>
        /// <param name="processRunner">The process runner for external commands.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="configuration">The configuration instance.</param>
        public DefaultBlueZDeviceManager(IDBusConnectionFactory dbusFactory, IProcessRunner processRunner, ILogger logger, BlueZNetConfiguration configuration)
        {
            _dbusFactory = dbusFactory ?? throw new ArgumentNullException(nameof(dbusFactory));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
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
                _logger.LogDebug("Starting device monitoring");

                _connection = await _dbusFactory.CreateSystemConnectionAsync(cancellationToken);
                _objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, BluezService, BluezPath);

                await LoadExistingDevicesAsync(cancellationToken);
                await SubscribeToDeviceChangesAsync();

                _isMonitoring = true;
                _logger.LogInformation("Device monitoring started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start device monitoring");
                await CleanupAsync();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
                return;

            _logger.LogDebug("Stopping device monitoring");
            await CleanupAsync();
            _isMonitoring = false;
            _logger.LogInformation("Device monitoring stopped");
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<BluetoothDevice>> GetConnectedDevicesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<BluetoothDevice>>(_connectedDevices.Values.ToList());
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<BluetoothDevice>> GetKnownDevicesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<BluetoothDevice>>(_knownDevices.Values.ToList());
        }

        /// <inheritdoc />
        public async Task<DeviceCapabilities> GetDeviceCapabilitiesAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_knownDevices.TryGetValue(deviceAddress, out var device))
                {
                    _logger.LogWarning("Device {DeviceAddress} not found", deviceAddress);
                    return new DeviceCapabilities();
                }

                if (_connection == null)
                    throw new InvalidOperationException("Connection not initialized");

                var deviceProxy = _dbusFactory.CreateProxy<IProperties>(_connection, BluezService, device.ObjectPath);
                var deviceProperties = await deviceProxy.GetAllAsync(DeviceInterface);

                var supportedProfiles = await GetSupportedProfilesAsync(deviceAddress, cancellationToken);
                var avrcpCaps = await GetAvrcpCapabilitiesAsync(device.ObjectPath, cancellationToken);
                var a2dpCaps = await GetA2dpCapabilitiesAsync(deviceAddress, cancellationToken);

                return new DeviceCapabilities(
                    avrcpCaps,
                    a2dpCaps,
                    supportedProfiles,
                    avrcpCaps.SupportsVolumeControl,
                    avrcpCaps.SupportsBrowsing,
                    deviceProperties.ContainsKey("SearchSupported") &&
                    deviceProperties["SearchSupported"] is bool searchSupported && searchSupported);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device capabilities for {DeviceAddress}", deviceAddress);
                return new DeviceCapabilities();
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AudioProfile>> GetSupportedProfilesAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_knownDevices.TryGetValue(deviceAddress, out var device))
                    return new List<AudioProfile>();

                if (_connection == null)
                    throw new InvalidOperationException("Connection not initialized");

                var deviceProxy = _dbusFactory.CreateProxy<IProperties>(_connection, BluezService, device.ObjectPath);
                var properties = await deviceProxy.GetAllAsync(DeviceInterface);

                var profiles = new List<AudioProfile>();

                if (properties.TryGetValue("UUIDs", out var uuidsObj) && uuidsObj is string[] uuids)
                {
                    foreach (var uuid in uuids)
                    {
                        switch (uuid.ToUpper())
                        {
                            case "0000110D-0000-1000-8000-00805F9B34FB": // A2DP
                                profiles.Add(AudioProfile.A2DP);
                                break;
                            case "0000111E-0000-1000-8000-00805F9B34FB": // HFP
                                profiles.Add(AudioProfile.HFP);
                                break;
                            case "00001108-0000-1000-8000-00805F9B34FB": // HSP
                                profiles.Add(AudioProfile.HSP);
                                break;
                            case "0000110E-0000-1000-8000-00805F9B34FB": // AVRCP
                                profiles.Add(AudioProfile.AVRCP);
                                break;
                        }
                    }
                }

                _logger.LogDebug("Device {DeviceAddress} supports profiles: {Profiles}",
                    deviceAddress, string.Join(", ", profiles));
                return profiles.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get supported profiles for {DeviceAddress}", deviceAddress);
                return new List<AudioProfile>();
            }
        }

        /// <inheritdoc />
        public async Task<AudioProfile?> GetActiveProfileAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_connectedDevices.TryGetValue(deviceAddress, out var device))
                {
                    _logger.LogDebug("Device {DeviceAddress} not found or not connected", deviceAddress);
                    return null;
                }

                if (_connection == null)
                    throw new InvalidOperationException("Connection not initialized");

                // Get device properties to check connected services
                var deviceProxy = _dbusFactory.CreateProxy<IProperties>(_connection, BluezService, device.ObjectPath);
                var properties = await deviceProxy.GetAllAsync(DeviceInterface);

                if (!properties.TryGetValue("Connected", out var connectedObj) || !(connectedObj is bool isConnected) || !isConnected)
                {
                    _logger.LogDebug("Device {DeviceAddress} is not connected", deviceAddress);
                    return null;
                }

                // Get connected services/UUIDs
                var connectedServices = new List<string>();
                if (properties.TryGetValue("UUIDs", out var uuidsObj) && uuidsObj is string[] uuids)
                {
                    connectedServices.AddRange(uuids);
                }

                // Check for active media transport (indicates A2DP is active)
                var hasActiveA2dpTransport = await HasActiveMediaTransportAsync(deviceAddress, cancellationToken);
                if (hasActiveA2dpTransport)
                {
                    _logger.LogDebug("Device {DeviceAddress} has active A2DP transport", deviceAddress);
                    return AudioProfile.A2DP;
                }

                // Check for active call/voice connection (indicates HFP/HSP)
                var activeVoiceProfile = await GetActiveVoiceProfileAsync(device.ObjectPath, connectedServices, cancellationToken);
                if (activeVoiceProfile.HasValue)
                {
                    _logger.LogDebug("Device {DeviceAddress} has active voice profile: {Profile}", deviceAddress, activeVoiceProfile);
                    return activeVoiceProfile;
                }

                // Check which profiles are available and connected
                var availableProfiles = await GetConnectedProfilesAsync(device.ObjectPath, connectedServices, cancellationToken);

                // Return the most likely active profile based on priority and availability
                if (availableProfiles.Contains(AudioProfile.A2DP))
                {
                    _logger.LogDebug("Device {DeviceAddress} defaulting to A2DP profile", deviceAddress);
                    return AudioProfile.A2DP;
                }

                if (availableProfiles.Contains(AudioProfile.HFP))
                {
                    _logger.LogDebug("Device {DeviceAddress} defaulting to HFP profile", deviceAddress);
                    return AudioProfile.HFP;
                }

                if (availableProfiles.Contains(AudioProfile.HSP))
                {
                    _logger.LogDebug("Device {DeviceAddress} defaulting to HSP profile", deviceAddress);
                    return AudioProfile.HSP;
                }

                _logger.LogDebug("No clear active profile detected for device {DeviceAddress}", deviceAddress);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active profile for {DeviceAddress}", deviceAddress);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SwitchProfileAsync(string deviceAddress, AudioProfile profile, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!_connectedDevices.TryGetValue(deviceAddress, out var device))
                {
                    _logger.LogWarning("Device {DeviceAddress} not found or not connected", deviceAddress);
                    return false;
                }

                // Check if the target profile is supported
                var supportedProfiles = await GetSupportedProfilesAsync(deviceAddress, cancellationToken);
                if (!supportedProfiles.Contains(profile))
                {
                    _logger.LogWarning("Profile {Profile} not supported by device {DeviceAddress}", profile, deviceAddress);
                    return false;
                }

                // Get current active profile
                var currentProfile = await GetActiveProfileAsync(deviceAddress, cancellationToken);
                if (currentProfile == profile)
                {
                    _logger.LogDebug("Profile {Profile} is already active for device {DeviceAddress}", profile, deviceAddress);
                    return true;
                }

                _logger.LogInformation("Switching device {DeviceAddress} from profile {CurrentProfile} to {TargetProfile}",
                    deviceAddress, currentProfile?.ToString() ?? "Unknown", profile);

                if (_connection == null)
                    throw new InvalidOperationException("Connection not initialized");

                var deviceProxy = _dbusFactory.CreateProxy<IDevice>(_connection, BluezService, device.ObjectPath);

                // Strategy 1: Try UUID-based connection for specific profile
                var success = await ConnectToSpecificProfileAsync(deviceProxy, deviceAddress, profile, cancellationToken);
                if (success)
                {
                    _logger.LogInformation("Successfully switched to profile {Profile} using UUID connection", profile);
                    return true;
                }

                // Strategy 2: Disconnect and reconnect to trigger profile renegotiation
                success = await ReconnectForProfileSwitchAsync(deviceProxy, deviceAddress, profile, cancellationToken);
                if (success)
                {
                    _logger.LogInformation("Successfully switched to profile {Profile} using reconnection", profile);
                    return true;
                }

                // Strategy 3: Use external bluetoothctl for complex profile switching
                success = await SwitchProfileUsingBluetoothctlAsync(deviceAddress, profile, cancellationToken);
                if (success)
                {
                    _logger.LogInformation("Successfully switched to profile {Profile} using bluetoothctl", profile);
                    return true;
                }

                _logger.LogWarning("Failed to switch device {DeviceAddress} to profile {Profile}", deviceAddress, profile);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to switch profile for device {DeviceAddress} to {Profile}", deviceAddress, profile);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsFeatureSupportedAsync(string deviceAddress, string feature, CancellationToken cancellationToken = default)
        {
            var capabilities = await GetDeviceCapabilitiesAsync(deviceAddress, cancellationToken);
            if (capabilities == null) return false;

            switch (feature.ToLower())
            {
                case "play":
                case "pause":
                case "stop":
                    return capabilities.Avrcp.SupportsPlayback;
                case "volume":
                    return capabilities.SupportsAbsoluteVolume;
                case "browsing":
                case "browse":
                    return capabilities.SupportsBrowsing;
                case "metadata":
                    return capabilities.Avrcp.SupportsMetadata;
                case "position":
                case "seek":
                    return capabilities.Avrcp.SupportsPosition;
                case "next":
                case "previous":
                    return capabilities.Avrcp.SupportedCommands.Contains("next") || capabilities.Avrcp.SupportedCommands.Contains("previous");
                case "search":
                    return capabilities.SupportsSearch;
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<MediaTransportInfo>> GetMediaTransportsAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            return await GetMediaTransportsForDeviceAsync(deviceAddress, cancellationToken);
        }

        /// <summary>
        /// Loads existing devices from BlueZ.
        /// </summary>
        private async Task LoadExistingDevicesAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_objectManager == null)
                    throw new InvalidOperationException("ObjectManager not initialized");

                var managedObjects = await _objectManager.GetManagedObjectsAsync();
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var kvp in managedObjects)
                {
                    var objectPath = kvp.Key;
                    var interfaces = kvp.Value;

                    if (interfaces.ContainsKey(DeviceInterface))
                    {
                        var device = await CreateBluetoothDeviceAsync(objectPath.ToString(), interfaces[DeviceInterface]);
                        if (device != null)
                        {
                            _knownDevices[device.Address] = device;
                            if (device.Connected)
                            {
                                _connectedDevices[device.Address] = device;
                            }

                            _logger.LogDebug("Loaded device: {Name} ({Address}) - Connected: {Connected}",
                                device.Name, device.Address, device.Connected);
                        }
                    }
                }

                _logger.LogInformation("Loaded {KnownCount} known devices, {ConnectedCount} currently connected",
                    _knownDevices.Count, _connectedDevices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load existing devices");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to BlueZ device changes.
        /// </summary>
        private async Task SubscribeToDeviceChangesAsync()
        {
            try
            {
                if (_objectManager == null) throw new InvalidOperationException("ObjectManager not initialized");
                    throw new InvalidOperationException("ObjectManager not initialized");

                var interfacesAddedSubscription = await _objectManager.WatchInterfacesAddedAsync(
                    args => SafeInvokeAsync(() => OnInterfacesAddedAsync(args)),
                    ex => _logger.LogError(ex, "Error in InterfacesAdded subscription"));
                _subscriptions.Add(interfacesAddedSubscription);

                var interfacesRemovedSubscription = await _objectManager.WatchInterfacesRemovedAsync(
                    args => SafeInvoke(() => OnInterfacesRemoved(args)),
                    ex => _logger.LogError(ex, "Error in InterfacesRemoved subscription"));
                _subscriptions.Add(interfacesRemovedSubscription);

                await SubscribeToExistingDevicePropertiesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to device changes");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to property changes on existing devices.
        /// </summary>
        private async Task SubscribeToExistingDevicePropertiesAsync()
        {
            foreach (var device in _knownDevices.Values.ToList())
            foreach (var device in devices)
            {
                try
                {
                    await SubscribeToDevicePropertiesAsync(device.ObjectPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to subscribe to properties for device {Address}", device.Address);
                }
            }
        }

        /// <summary>
        /// Subscribes to property changes for a specific device.
        /// </summary>
        private async Task SubscribeToDevicePropertiesAsync(string objectPath)
        {
            try
            {
                if (_connection == null) throw new InvalidOperationException("Connection not initialized");
                    throw new InvalidOperationException("Connection not initialized");

                var properties = _dbusFactory.CreateProxy<IProperties>(_connection, BluezService, objectPath);
                var subscription = await properties.WatchPropertiesChangedAsync(
                    change => SafeInvokeAsync(() => OnDevicePropertyChangedAsync(objectPath, change)),
                    ex => _logger.LogError(ex, "Error in device property subscription for {ObjectPath}", objectPath));

                _subscriptions.Add(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to properties for {ObjectPath}", objectPath);
            }
        }

        /// <summary>
        /// Handles new BlueZ interfaces being added.
        /// </summary>
        private async Task OnInterfacesAddedAsync((ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfacesAndProperties) args)
        {
            try
            {
                if (args.interfacesAndProperties.ContainsKey(DeviceInterface))
                {
                    var device = await CreateBluetoothDeviceAsync(args.objectPath.ToString(), args.interfacesAndProperties[DeviceInterface]);
                    if (device != null)
                    {
                        _knownDevices[device.Address] = device;
                        _logger.LogInformation("New device discovered: {Name} ({Address})", device.Name, device.Address);

                        await SubscribeToDevicePropertiesAsync(args.objectPath.ToString());

                        if (device.Connected)
                        {
                            _connectedDevices[device.Address] = device;
                            DeviceConnectionChanged?.Invoke(this, new BluetoothConnectionEventArgs(device, true));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling InterfacesAdded for {ObjectPath}", args.objectPath);
            }
        }

        /// <summary>
        /// Handles BlueZ interfaces being removed.
        /// </summary>
        private void OnInterfacesRemoved((ObjectPath objectPath, string[] interfaces) args)
        {
            try
            {
                if (args.interfaces.Contains(DeviceInterface))
                {
                    var deviceToRemove = _knownDevices.Values.FirstOrDefault(d => d.ObjectPath == args.objectPath.ToString());
                    if (deviceToRemove != null)
                    {
                        _knownDevices.TryRemove(deviceToRemove.Address, out _);
                        var wasConnected = _connectedDevices.TryRemove(deviceToRemove.Address, out _);
                        _deviceAddressToObjectPath.TryRemove(deviceToRemove.Address, out _);

                        _logger.LogInformation("Device removed: {Name} ({Address})", deviceToRemove.Name, deviceToRemove.Address);

                        if (wasConnected)
                        {
                            DeviceConnectionChanged?.Invoke(this, new BluetoothConnectionEventArgs(deviceToRemove, false));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling InterfacesRemoved for {ObjectPath}", args.objectPath);
            }
        }

        /// <summary>
        /// Handles device property changes. Refreshes the device state and raises events.
        /// </summary>
        private async Task OnDevicePropertyChangedAsync(string objectPath, (string interfaceName, IDictionary<string, object> changedProperties, string[] invalidatedProperties) change)
        {
            try
            {
                var device = _knownDevices.Values.FirstOrDefault(d => d.ObjectPath == objectPath);
                if (device == null) return;

                bool wasConnected = device.Connected;
                bool isConnected = wasConnected;

                if (change.interfaceName == DeviceInterface && change.changedProperties.ContainsKey("Connected"))
                {
                    isConnected = (bool)change.changedProperties["Connected"];
                }

                // Refresh the entire device object to capture all changes
                var updatedDevice = await CreateBluetoothDeviceAsync(objectPath, null);
                if (updatedDevice == null) return;

                _knownDevices[device.Address] = updatedDevice;

                if (updatedDevice.Connected)
                {
                    _connectedDevices[device.Address] = updatedDevice;
                    if (!wasConnected)
                    {
                        _logger.LogInformation("Device connected: {Name} ({Address})", updatedDevice.Name, updatedDevice.Address);
                        DeviceConnectionChanged?.Invoke(this, new BluetoothConnectionEventArgs(updatedDevice, true));
                    }
                }
                else
                {
                    _connectedDevices.TryRemove(device.Address, out _);
                    if (wasConnected)
                    {
                        _logger.LogInformation("Device disconnected: {Name} ({Address})", updatedDevice.Name, updatedDevice.Address);
                        DeviceConnectionChanged?.Invoke(this, new BluetoothConnectionEventArgs(updatedDevice, false));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling property change for {ObjectPath}", objectPath);
            }
        }

        /// <summary>
        /// Creates a BluetoothDevice instance from BlueZ D-Bus properties.
        /// If properties are null, they will be fetched from D-Bus.
        /// </summary>
        private async Task<BluetoothDevice> CreateBluetoothDeviceAsync(string objectPath, IDictionary<string, object> properties)
        {
            try
            {
                if (properties == null)
                {
                    var propsProxy = _dbusFactory.CreateProxy<IProperties>(_connection, BluezService, objectPath);
                    properties = await propsProxy.GetAllAsync(DeviceInterface);
                }

                if (!properties.TryGetValue("Address", out var addressObj) || !(addressObj is string address)) return null;
                    return null;

                // Track the address to object path mapping
                _deviceAddressToObjectPath[address] = objectPath;

                var name = properties.TryGetValue("Alias", out var aliasObj) && aliasObj is string aliasStr && !string.IsNullOrEmpty(aliasStr)
                    ? aliasStr
                    : properties.TryGetValue("Name", out var nameObj) && nameObj is string nameStr ? nameStr : address;

                var connected = properties.TryGetValue("Connected", out var connectedObj) && connectedObj is bool connectedBool && connectedBool;

                // Get device capabilities if connected
                var capabilities = connected ? await GetDeviceCapabilitiesAsync(address) : new DeviceCapabilities();
                var activeProfile = connected ? await GetActiveProfileAsync(address) : null;

                return new BluetoothDevice(address, name, objectPath, connected, DateTime.UtcNow, null, null, null, capabilities, activeProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create BluetoothDevice from properties for {ObjectPath}", objectPath);
                return null;
            }
        }

        /// <summary>
        /// Checks if the device has an active media transport (A2DP streaming).
        /// </summary>
        private async Task<bool> HasActiveMediaTransportAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            try
            {
                var transports = await GetMediaTransportsForDeviceAsync(deviceAddress, cancellationToken);
                return transports.Any(t => t.State == "active" || t.State == "pending");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to check media transport for device {DeviceAddress}", deviceAddress);
                return false;
            }
        }

        /// <summary>
        /// Gets the active voice profile (HFP/HSP) if any.
        /// </summary>
        private async Task<AudioProfile?> GetActiveVoiceProfileAsync(string deviceObjectPath, List<string> connectedServices, CancellationToken cancellationToken)
        {
            try
            {
                if (_connection == null)
                    return null;

                // Check for voice profile UUIDs
                var hasHfp = connectedServices.Any(uuid =>
                    uuid.Equals("0000111E-0000-1000-8000-00805F9B34FB", StringComparison.OrdinalIgnoreCase));
                var hasHsp = connectedServices.Any(uuid =>
                    uuid.Equals("00001108-0000-1000-8000-00805F9B34FB", StringComparison.OrdinalIgnoreCase));

                if (!hasHfp && !hasHsp)
                    return null;

                // Check for active voice connections
                var hasActiveSco = await CheckForActiveScoConnectionAsync(deviceObjectPath, cancellationToken);
                var hasActiveCall = await CheckForActiveCallAsync(deviceObjectPath, cancellationToken);

                if (hasActiveSco || hasActiveCall)
                {
                    // Prefer HFP over HSP if both are available
                    return hasHfp ? AudioProfile.HFP : AudioProfile.HSP;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get active voice profile for device {DeviceObjectPath}", deviceObjectPath);
                return null;
            }
        }

        /// <summary>
        /// Checks for active SCO (voice) connections.
        /// </summary>
        private async Task<bool> CheckForActiveScoConnectionAsync(string deviceObjectPath, CancellationToken cancellationToken)
        {
            try
            {
                if (_connection == null)
                    return false;

                // Check for AudioGateway interface which indicates HFP/HSP capability
                var objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, BluezService, "/");
                var managedObjects = await objectManager.GetManagedObjectsAsync();

                foreach (var kvp in managedObjects)
                {
                    var objectPath = kvp.Key.ToString();
                    var interfaces = kvp.Value;

                    // Look for AudioGateway interfaces related to our device
                    if (interfaces.ContainsKey("org.bluez.AudioGateway1") &&
                        objectPath.StartsWith(deviceObjectPath))
                    {
                        var properties = interfaces["org.bluez.AudioGateway1"];

                        // Check if there's an active connection
                        if (properties.TryGetValue("Connected", out var connectedObj) &&
                            connectedObj is bool connected && connected)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Failed to check SCO connection for device {DeviceObjectPath}", deviceObjectPath);
                return false;
            }
        }

        /// <summary>
        /// Checks for active call state.
        /// </summary>
        private async Task<bool> CheckForActiveCallAsync(string deviceObjectPath, CancellationToken cancellationToken)
        {
            try
            {
                if (_connection == null)
                    return false;

                // Check for Telephony interface which indicates call management capability
                var objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, BluezService, "/");
                var managedObjects = await objectManager.GetManagedObjectsAsync();

                foreach (var kvp in managedObjects)
                {
                    var objectPath = kvp.Key.ToString();
                    var interfaces = kvp.Value;

                    // Look for Telephony interfaces related to our device
                    if (interfaces.ContainsKey("org.bluez.Telephony1") &&
                        objectPath.StartsWith(deviceObjectPath))
                    {
                        var properties = interfaces["org.bluez.Telephony1"];

                        // Check for active call indicators
                        if (properties.TryGetValue("CallActive", out var callActiveObj) &&
                            callActiveObj is bool callActive && callActive)
                        {
                            return true;
                        }

                        if (properties.TryGetValue("CallSetup", out var callSetupObj) &&
                            callSetupObj is string callSetup && callSetup != "inactive")
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Failed to check call state for device {DeviceObjectPath}", deviceObjectPath);
                return false;
            }
        }

        /// <summary>
        /// Gets the list of currently connected profiles for a device.
        /// </summary>
        private async Task<List<AudioProfile>> GetConnectedProfilesAsync(string deviceObjectPath, List<string> connectedServices, CancellationToken cancellationToken)
        {
            var profiles = new List<AudioProfile>();

            try
            {
                // Check supported UUIDs
                foreach (var uuid in connectedServices)
                {
                    switch (uuid.ToUpper())
                    {
                        case "0000110D-0000-1000-8000-00805F9B34FB": // A2DP
                            if (!profiles.Contains(AudioProfile.A2DP))
                                profiles.Add(AudioProfile.A2DP);
                            break;
                        case "0000111E-0000-1000-8000-00805F9B34FB": // HFP
                            if (!profiles.Contains(AudioProfile.HFP))
                                profiles.Add(AudioProfile.HFP);
                            break;
                        case "00001108-0000-1000-8000-00805F9B34FB": // HSP
                            if (!profiles.Contains(AudioProfile.HSP))
                                profiles.Add(AudioProfile.HSP);
                            break;
                        case "0000110E-0000-1000-8000-00805F9B34FB": // AVRCP
                            if (!profiles.Contains(AudioProfile.AVRCP))
                                profiles.Add(AudioProfile.AVRCP);
                            break;
                    }
                }

                await Task.CompletedTask; // Prevent compiler warning
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get connected profiles for device {DeviceObjectPath}", deviceObjectPath);
            }

            return profiles;
        }

        /// <summary>
        /// Attempts to connect to a specific profile using UUID-based connection.
        /// </summary>
        private async Task<bool> ConnectToSpecificProfileAsync(IDevice deviceProxy, string deviceAddress, AudioProfile profile, CancellationToken cancellationToken)
        {
            try
            {
                // Get the UUID for the target profile
                var profileUuid = GetProfileUuid(profile);
                if (profileUuid == null)
                    return false;

                _logger.LogDebug("Attempting UUID-based connection to profile {Profile} for device {DeviceAddress}", profile, deviceAddress);

                // Try to connect to the device - BlueZ will negotiate profiles automatically
                await deviceProxy.ConnectAsync();
                await Task.Delay(3000, cancellationToken); // Give time for profile negotiation

                // Verify the profile is now active
                var activeProfile = await GetActiveProfileAsync(deviceAddress, cancellationToken);
                var success = activeProfile == profile;

                _logger.LogDebug("UUID-based connection result: {Success}, active profile: {ActiveProfile}",
                    success, activeProfile?.ToString() ?? "Unknown");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "UUID-based profile connection failed for profile {Profile}", profile);
                return false;
            }
        }

        /// <summary>
        /// Attempts profile switching by disconnecting and reconnecting the device.
        /// </summary>
        private async Task<bool> ReconnectForProfileSwitchAsync(IDevice deviceProxy, string deviceAddress, AudioProfile profile, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Attempting profile switch via reconnection for profile {Profile}", profile);

                await deviceProxy.DisconnectAsync();
                await Task.Delay(_configuration.ProfileSwitchDisconnectDelayMs, cancellationToken);

                await deviceProxy.ConnectAsync();
                await Task.Delay(_configuration.ProfileSwitchConnectDelayMs, cancellationToken);

                var activeProfile = await GetActiveProfileAsync(deviceAddress, cancellationToken);
                var success = activeProfile == profile;

                _logger.LogDebug("Reconnection profile switch result: {Success}, active profile: {ActiveProfile}",
                    success, activeProfile?.ToString() ?? "Unknown");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to switch profile via reconnection for profile {Profile}", profile);
                return false;
            }
        }

        /// <summary>
        /// Attempts profile switching using external bluetoothctl command.
        /// </summary>
        private async Task<bool> SwitchProfileUsingBluetoothctlAsync(string deviceAddress, AudioProfile profile, CancellationToken cancellationToken)
        {
            try
            {
                if (!await _processRunner.CommandExistsAsync("bluetoothctl"))
                {
                    _logger.LogDebug("bluetoothctl command not found");
                    return false;
                }

                _logger.LogDebug("Attempting profile switch using bluetoothctl for profile {Profile}", profile);

                // Get the profile name for bluetoothctl
                var profileName = GetBluetoothctlProfileName(profile);
                if (profileName == null)
                {
                    _logger.LogDebug("No bluetoothctl profile name available for {Profile}", profile);
                    return false;
                }

                // Disconnect from all profiles first
                var disconnectResult = await _processRunner.RunAsync("bluetoothctl", $"disconnect {deviceAddress}", cancellationToken);
                if (!disconnectResult.Success)
                {
                    _logger.LogWarning("Failed to disconnect device {DeviceAddress}: {Error}", deviceAddress, disconnectResult.StandardError);
                }

                // Wait for clean disconnection
                await Task.Delay(3000, cancellationToken);

                // Connect using specific profile
                var connectResult = await _processRunner.RunAsync("bluetoothctl", $"connect {deviceAddress}", cancellationToken);
                if (!connectResult.Success)
                {
                    _logger.LogWarning("Failed to connect device {DeviceAddress}: {Error}", deviceAddress, connectResult.StandardError);
                    return false;
                }

                // Wait for connection establishment
                await Task.Delay(5000, cancellationToken);

                // Verify the profile is now active
                var activeProfile = await GetActiveProfileAsync(deviceAddress, cancellationToken);
                var success = activeProfile == profile;

                _logger.LogDebug("bluetoothctl profile switch result: {Success}, active profile: {ActiveProfile}",
                    success, activeProfile?.ToString() ?? "Unknown");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to switch profile using bluetoothctl for profile {Profile}", profile);
                return false;
            }
        }

        /// <summary>
        /// Gets AVRCP capabilities for a device.
        /// </summary>
        private Task<AvrcpCapabilities> GetAvrcpCapabilitiesAsync(string devicePath)
        {
            // Simplified implementation - would need more sophisticated capability detection
            var supportedCommands = new List<string> { "play", "pause", "next", "previous" };
            var capabilities = new AvrcpCapabilities("1.4", supportedCommands, true, false, false, true, false);
            return Task.FromResult(capabilities);
        }

        /// <summary>
        /// Gets A2DP capabilities for a device.
        /// </summary>
        private async Task<A2dpCapabilities> GetA2dpCapabilitiesAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            try
            {
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
                _logger.LogError(ex, "Failed to get A2DP capabilities for device {DeviceAddress}", deviceAddress);
                return new A2dpCapabilities();
            }
        }

        #region A2DP Capability Helpers

        private async Task<List<MediaTransportInfo>> GetMediaTransportsForDeviceAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            try
            {
                if (_connection == null) await StartMonitoringAsync(cancellationToken);

                var objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, BluezService, "/");
                var managedObjects = await objectManager.GetManagedObjectsAsync();

                var transports = new List<MediaTransportInfo>();
                var deviceMacForPath = deviceAddress.Replace(":", "_");

                foreach (var kvp in managedObjects)
                {
                    if (kvp.Value.ContainsKey("org.bluez.MediaTransport1") && kvp.Key.ToString().Contains(deviceMacForPath))
                    {
                        var transport = ParseMediaTransportInfo(kvp.Key.ToString(), kvp.Value["org.bluez.MediaTransport1"]);
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

        private async Task<List<MediaEndpointInfo>> GetMediaEndpointsForDeviceAsync(string deviceAddress, CancellationToken cancellationToken)
        {
            try
            {
                if (_connection == null) await StartMonitoringAsync(cancellationToken);

                var objectManager = _dbusFactory.CreateProxy<IObjectManager>(_connection, BluezService, "/");
                var managedObjects = await objectManager.GetManagedObjectsAsync();

                var endpoints = new List<MediaEndpointInfo>();
                var deviceMacForPath = deviceAddress.Replace(":", "_");

                foreach (var kvp in managedObjects)
                {
                    if (kvp.Value.ContainsKey("org.bluez.MediaEndpoint1") && kvp.Key.ToString().Contains(deviceMacForPath))
                    {
                        var endpoint = ParseMediaEndpointInfo(kvp.Key.ToString(), kvp.Value["org.bluez.MediaEndpoint1"]);
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

        private MediaTransportInfo ParseMediaTransportInfo(string objectPath, IDictionary<string, object> properties)
        {
            try
            {
                return new MediaTransportInfo(
                    objectPath,
                    properties.TryGetValue("Device", out var dev) ? dev.ToString() : null,
                    properties.TryGetValue("UUID", out var uuid) ? uuid.ToString() : null,
                    properties.TryGetValue("Codec", out var c) ? (byte)c : (byte)0,
                    properties.TryGetValue("State", out var s) ? s.ToString() : "idle",
                    properties.TryGetValue("Volume", out var v) ? (ushort)v : (ushort)0,
                    properties.TryGetValue("Configuration", out var cfg) ? (byte[])cfg : null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MediaTransport info for {ObjectPath}", objectPath);
                return null;
            }
        }

        private MediaEndpointInfo ParseMediaEndpointInfo(string objectPath, IDictionary<string, object> properties)
        {
            try
            {
                return new MediaEndpointInfo(
                    objectPath,
                    properties.TryGetValue("UUID", out var uuid) ? uuid.ToString() : null,
                    properties.TryGetValue("Codec", out var c) ? (byte)c : (byte)0,
                    properties.TryGetValue("Capabilities", out var caps) ? (byte[])caps : null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse MediaEndpoint info for {ObjectPath}", objectPath);
                return null;
            }
        }

        private Task<List<AudioCodec>> ParseCodecsFromEndpointAsync(MediaEndpointInfo endpoint, CancellationToken cancellationToken)
        {
            var codecs = new List<AudioCodec>();
            try
            {
                var codecInfo = ParseA2dpCodecInfo(endpoint.Codec, endpoint.Capabilities);
                if (codecInfo != null) codecs.Add(codecInfo);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse codec from endpoint {ObjectPath}", endpoint.ObjectPath);
            }
            return Task.FromResult(codecs);
        }

        private Task<AudioCodec> GetCodecFromTransportAsync(MediaTransportInfo transport, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(ParseA2dpCodecInfo(transport.Codec, transport.Configuration));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get codec from transport {ObjectPath}", transport.ObjectPath);
                return Task.FromResult<AudioCodec>(null);
            }
        }

        private AudioCodec ParseA2dpCodecInfo(byte codecId, byte[] configuration)
        {
            switch (codecId)
            {
                case 0x00: return new AudioCodec("SBC", 328, "Standard");
                case 0x01: return new AudioCodec("MP3", 320, "Standard");
                case 0x02: return new AudioCodec("AAC", 320, "High");
                case 0x40: return new AudioCodec("aptX", 352, "High");
                case 0x41: return new AudioCodec("aptX HD", 576, "Lossless");
                case 0xAA: return new AudioCodec("LDAC", 990, "Lossless");
                default: return new AudioCodec($"Unknown (0x{codecId:X2})", 0, "Unknown");
            }
        }

        #endregion

        /// <summary>
        /// Gets the UUID string for the specified audio profile.
        /// </summary>
        private string GetProfileUuid(AudioProfile profile)
        {
            switch (profile)
            {
                case AudioProfile.A2DP:
                    return "0000110D-0000-1000-8000-00805F9B34FB";
                case AudioProfile.HFP:
                    return "0000111E-0000-1000-8000-00805F9B34FB";
                case AudioProfile.HSP:
                    return "00001108-0000-1000-8000-00805F9B34FB";
                case AudioProfile.AVRCP:
                    return "0000110E-0000-1000-8000-00805F9B34FB";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the bluetoothctl profile name for the specified audio profile.
        /// </summary>
        private string GetBluetoothctlProfileName(AudioProfile profile)
        {
            switch (profile)
            {
                case AudioProfile.A2DP:
                    return "a2dp";
                case AudioProfile.HFP:
                    return "hfp";
                case AudioProfile.HSP:
                    return "hsp";
                case AudioProfile.AVRCP:
                    return "avrcp";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Safely invokes an action with exception handling.
        /// </summary>
        private void SafeInvoke(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler");
            }
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
        /// Cleans up resources and subscriptions.
        /// </summary>
        private async Task CleanupAsync()
        {
            try
            {
                foreach (var subscription in _subscriptions)
                {
                    try
                    {
                        subscription?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing subscription");
                    }
                }
                _subscriptions.Clear();

                if (_connection != null)
                {
                    await _dbusFactory.DisposeConnectionAsync(_connection);
                    _connection = null;
                }

                _objectManager = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        /// <summary>
        /// Releases all resources used by the DefaultBlueZDeviceManager.
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