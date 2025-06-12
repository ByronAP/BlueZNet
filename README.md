# BlueZNet

[![.NET](https://img.shields.io/badge/NETSTANDARD-2.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Linux-FCC624?style=flat&logo=linux&logoColor=black)](https://www.linux.org/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A comprehensive .NET library for controlling and automating Bluetooth devices on Linux systems. Designed for advanced Bluetooth audio applications on Raspberry Pi and other Linux platforms using BlueZ and PulseAudio.

## 🎯 Features

### **Device Monitoring**
- **Real-time device detection** - Automatic discovery of connecting/disconnecting devices
- **Comprehensive device information** - Names, addresses, capabilities, and connection status
- **Smart capability detection** - Knows what each device supports before attempting operations
- **Profile management** - A2DP, HFP, HSP, and AVRCP profile detection and switching

### **Media Control (AVRCP)**
- **Playback control** - Play, pause, stop, next, previous, fast forward, rewind
- **Volume control** - Both device-side (AVRCP) and sink-side (PulseAudio) volume management
- **Position control** - Seek to specific track positions with millisecond precision
- **Playback modes** - Shuffle and repeat mode control
- **Real-time metadata** - Track title, artist, album, duration, and position updates

### **Media Browsing**
- **Library exploration** - Browse device music libraries when supported
- **Folder navigation** - Artists, albums, playlists, and genre folders
- **Direct playback** - Play specific tracks or add them to queue
- **Smart filtering** - Automatically detects apps that don't support browsing (e.g., YouTube Music)

### **Audio Processing**
- **Dynamic range compression** - Real-time audio compression using LADSPA plugins
- **Noise gating** - Automatic background noise elimination
- **Audio format detection** - Sample rate, bit depth, and codec information
- **Multi-codec support** - SBC, aptX, AAC, LDAC codec detection and switching

### **Advanced Features**
- **Event-driven architecture** - Comprehensive event system for all state changes
- **Thread-safe operations** - Concurrent access support with proper synchronization
- **Cancellation support** - All async operations support CancellationToken
- **Comprehensive logging** - Microsoft.Extensions.Logging integration
- **Exception handling** - Detailed error reporting with custom exception types
- **Resource management** - Proper disposal patterns and cleanup

## 🔧 Requirements

### **System Requirements**
- **Operating System**: Linux (tested on Raspberry Pi OS, Ubuntu, Debian)
- **.NET Runtime**: .NET SDK / Runtime
- **Bluetooth Stack**: BlueZ 5.50+ with experimental features enabled
- **Audio System**: PulseAudio with system mode configuration

### **Dependencies**
- `Tmds.DBus` - D-Bus communication with BlueZ
- `Microsoft.Extensions.Logging.Abstractions` - Logging framework

### **Optional Dependencies**
- **LADSPA plugins** - For audio processing features
  ```bash
  sudo apt install ladspa-sdk swh-plugins cmt
  ```

## 📦 Installation

### **Via NuGet Package Manager**
```bash
dotnet add package BlueZNet
```

### **Via Package Manager Console**
```powershell
Install-Package BlueZNet
```

### **Manual Installation**
1. Clone the repository:
   ```bash
   git clone https://github.com/ByronAP/BlueZNet.git
   cd BlueZNet
   ```

2. Build the library:
   ```bash
   dotnet build --configuration Release
   ```

3. Reference in your project:
   ```xml
   <ProjectReference Include="path/to/BlueZNet/BlueZNet.csproj" />
   ```

## 🚀 Quick Start

### **Basic Device Monitoring**

```csharp
using BlueZNet.Services;
using BlueZNet.Interfaces;
using Microsoft.Extensions.Logging;

// Create logger (optional)
using var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<BlueZNetControllerService>();

// Create and configure controller
using var controller = new BlueZNetControllerService(logger);

// Subscribe to device events
controller.DeviceConnectionChanged += (sender, e) =>
{
    var status = e.IsConnected ? "connected" : "disconnected";
    Console.WriteLine($"Device {status}: {e.Device.Name} ({e.Device.Address})");
};

// Start monitoring with cancellation support
using var cts = new CancellationTokenSource();
await controller.StartMonitoringAsync(cts.Token);

// Keep running
Console.WriteLine("Press any key to stop...");
Console.ReadKey();

// Graceful shutdown
await controller.StopMonitoringAsync();
```

### **Media Control Example**

```csharp
// Get connected devices
var devices = await controller.GetConnectedDevicesAsync();
var phone = devices.FirstOrDefault();

if (phone != null)
{
    // Check capabilities before attempting operations
    var caps = await controller.GetDeviceCapabilitiesAsync(phone.Address);
    
    if (caps.Avrcp.SupportsPlayback)
    {
        // Control playback with timeout
        using var playbackCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        await controller.PlayAsync(phone.Address, playbackCts.Token);
        await Task.Delay(5000, playbackCts.Token);
        await controller.PauseAsync(phone.Address, playbackCts.Token);
        
        // Control volume (0.0 to 1.0)
        await controller.SetVolumeAsync(phone.Address, 0.7, playbackCts.Token);
        
        // Skip tracks
        await controller.NextTrackAsync(phone.Address, playbackCts.Token);
    }
    
    if (caps.SupportsBrowsing)
    {
        // Browse media library with cancellation
        var folders = await controller.BrowseMediaAsync(phone.Address, cancellationToken: cts.Token);
        foreach (var folder in folders)
        {
            Console.WriteLine($"📁 {folder.Name} ({folder.Type})");
        }
    }
}
```

### **Real-time Media Information**

```csharp
// Subscribe to media events
controller.MediaPlaybackChanged += (sender, e) =>
{
    var track = e.MediaPlayer.Track;
    if (track != null)
    {
        Console.WriteLine($"Now playing: {track.Title} by {track.Artist}");
        
        if (track.Duration.HasValue && e.MediaPlayer.Position.HasValue)
        {
            var duration = TimeSpan.FromMilliseconds(track.Duration.Value);
            var position = TimeSpan.FromMilliseconds(e.MediaPlayer.Position.Value);
            Console.WriteLine($"Position: {position:mm\\:ss} / {duration:mm\\:ss}");
        }
    }
    
    Console.WriteLine($"Status: {e.MediaPlayer.Status}"); // playing, paused, stopped
};

// Subscribe to audio stream events
controller.AudioStreamChanged += (sender, e) =>
{
    var stream = e.AudioStream;
    Console.WriteLine($"Audio: {(stream.IsActive ? "Playing" : "Idle")} " +
                     $"Volume: {stream.Volume:P0} " +
                     $"Format: {stream.Format} @ {stream.SampleRate}Hz");
};
```

### **Advanced Audio Processing**

```csharp
// Enable dynamic range compression with cancellation support
var compressionSettings = new AudioProcessingSettings(
    dynamicRangeCompressionEnabled: true,
    compressionRatio: 4.0,         // 4:1 compression
    thresholdDb: -12.0,            // Compress signals above -12dB
    attackMs: 5.0,                 // 5ms attack time
    releaseMs: 100.0,              // 100ms release time
    noiseGateEnabled: true,
    noiseGateThresholdDb: -60.0    // Gate signals below -60dB
);

using var processingCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await controller.SetDynamicRangeCompressionAsync(deviceAddress, compressionSettings, processingCts.Token);
```

## 🔧 Configuration

### **BlueZ Setup**
The library requires BlueZ with experimental features enabled. See our [setup guide](docs/setup.md) for complete configuration instructions.

**Key requirements:**
- BlueZ experimental features: `bluetoothd --experimental`
- PulseAudio system mode
- Proper D-Bus permissions
- AVRCP with absolute volume control

### **Logging Configuration**

```csharp
// Detailed logging
using var loggerFactory = LoggerFactory.Create(builder => 
    builder
        .AddConsole()
        .AddFile("bluetooth.log") // If using Serilog
        .SetMinimumLevel(LogLevel.Debug));

var controller = new BlueZNetControllerService(loggerFactory.CreateLogger<BlueZNetControllerService>());
```

### **Dependency Injection**

```csharp
// In Startup.cs or Program.cs
services.AddSingleton<IBlueZNetController, BlueZNetControllerService>();

// Usage
public class AudioController : ControllerBase
{
    private readonly IBlueZNetController _bluetoothController;
    
    public AudioController(IBlueZNetController bluetoothController)
    {
        _bluetoothController = bluetoothController;
    }
    
    [HttpPost("play/{deviceAddress}")]
    public async Task<IActionResult> Play(string deviceAddress, CancellationToken cancellationToken)
    {
        var success = await _bluetoothController.PlayAsync(deviceAddress, cancellationToken);
        return success ? Ok() : BadRequest("Failed to start playback");
    }
}
```

## 📋 API Reference

### **Core Interfaces**

#### `IBlueZNetController`
Primary interface for all Bluetooth operations.

**Events:**
- `DeviceConnectionChanged` - Device connects/disconnects
- `MediaPlaybackChanged` - Media state changes (play/pause/track change)
- `AudioStreamChanged` - Audio stream state changes (volume/format)

**Methods:**
- `StartMonitoringAsync(CancellationToken)` - Begin monitoring devices
- `GetConnectedDevicesAsync(CancellationToken)` - Get currently connected devices
- `GetDeviceCapabilitiesAsync(string, CancellationToken)` - Check device capabilities

#### **Media Control Methods**
```csharp
Task<bool> PlayAsync(string deviceAddress, CancellationToken cancellationToken = default);
Task<bool> PauseAsync(string deviceAddress, CancellationToken cancellationToken = default);
Task<bool> NextTrackAsync(string deviceAddress, CancellationToken cancellationToken = default);
Task<bool> SetVolumeAsync(string deviceAddress, double volume, CancellationToken cancellationToken = default);
Task<bool> SeekAsync(string deviceAddress, uint positionMs, CancellationToken cancellationToken = default);
```

#### **Media Browsing Methods**
```csharp
Task<IReadOnlyList<MediaFolder>> BrowseMediaAsync(string deviceAddress, string folderPath = null, CancellationToken cancellationToken = default);
Task<IReadOnlyList<MediaItem>> GetFolderItemsAsync(string deviceAddress, string folderPath, CancellationToken cancellationToken = default);
Task<bool> PlayItemAsync(string deviceAddress, string itemPath, CancellationToken cancellationToken = default);
```

#### **Device Management Methods**
```csharp
Task<DeviceCapabilities> GetDeviceCapabilitiesAsync(string deviceAddress, CancellationToken cancellationToken = default);
Task<IReadOnlyList<AudioProfile>> GetSupportedProfilesAsync(string deviceAddress, CancellationToken cancellationToken = default);
Task<bool> SwitchProfileAsync(string deviceAddress, AudioProfile profile, CancellationToken cancellationToken = default);
```

### **Data Models**

#### `BluetoothDevice`
Complete device information including capabilities, media state, and audio stream details.

#### `DeviceCapabilities`
Comprehensive capability information:
- AVRCP version and supported commands
- A2DP codecs and audio quality
- Supported Bluetooth profiles
- Feature support flags

#### `MediaPlayerInfo`
Current media player state:
- Playback status (playing/paused/stopped)
- Track metadata (title, artist, album, duration)
- Position information
- Shuffle and repeat settings

#### `AudioStreamInfo`
Real-time audio stream information:
- Active/idle status
- Volume and mute state
- Audio format and sample rate
- PulseAudio sink details

## 🎯 Use Cases

### **Smart Home Audio Systems**
```csharp
// Automatically pause music when doorbell rings
controller.DeviceConnectionChanged += async (s, e) =>
{
    if (e.IsConnected && e.Device.Name.Contains("Doorbell"))
    {
        var audioDevices = await controller.GetConnectedDevicesAsync();
        foreach (var device in audioDevices.Where(d => d.MediaPlayer?.Status == "playing"))
        {
            await controller.PauseAsync(device.Address);
        }
    }
};
```

### **Multi-Room Audio Control**
```csharp
// Synchronize playback across multiple devices with timeout
public async Task SynchronizePlayback(List<string> deviceAddresses, CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromSeconds(5));
    
    var tasks = deviceAddresses.Select(addr => controller.PlayAsync(addr, cts.Token));
    await Task.WhenAll(tasks);
}
```

### **Voice Control Integration**
```csharp
// Voice command processing with cancellation
public async Task ProcessVoiceCommand(string command, string deviceName, CancellationToken cancellationToken)
{
    var devices = await controller.GetConnectedDevicesAsync(cancellationToken);
    var device = devices.FirstOrDefault(d => d.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase));
    if (device == null) return;
    
    switch (command.ToLower())
    {
        case "play music":
            await controller.PlayAsync(device.Address, cancellationToken);
            break;
        case "volume up":
            var currentVolume = await controller.GetVolumeAsync(device.Address, cancellationToken) ?? 0.5;
            await controller.SetVolumeAsync(device.Address, Math.Min(1.0, currentVolume + 0.1), cancellationToken);
            break;
        case "next song":
            await controller.NextTrackAsync(device.Address, cancellationToken);
            break;
    }
}
```

### **Audio Analytics**
```csharp
// Track listening patterns
private readonly Dictionary<string, DateTime> _listeningTime = new();

controller.MediaPlaybackChanged += (s, e) =>
{
    if (e.MediaPlayer.Status == "playing")
        _listeningTime[e.Device.Address] = DateTime.UtcNow;
    else if (e.MediaPlayer.Status == "paused" && _listeningTime.ContainsKey(e.Device.Address))
    {
        var session = DateTime.UtcNow - _listeningTime[e.Device.Address];
        LogListeningSession(e.Device.Name, session);
    }
};
```

## 🔍 Device Compatibility

### **Fully Supported**
- ✅ **Modern Android devices** (Android 8+) - Full AVRCP 1.6 support
- ✅ **iPhones** (iOS 12+) - Complete media control and metadata
- ✅ **Local music apps** - Full browsing and control capabilities
- ✅ **Bluetooth headphones/speakers** - When used as source devices

### **Partially Supported**
- ⚠️ **YouTube Music** - Playback control only, no library browsing
- ⚠️ **Spotify** - Limited browsing, full playback control
- ⚠️ **Older devices** - Basic AVRCP 1.3 support (no browsing)

### **Feature Detection**
```csharp
var caps = await controller.GetDeviceCapabilitiesAsync(deviceAddress);

// Check before attempting operations
if (caps.SupportsBrowsing)
    Console.WriteLine("✅ Can browse music library");
else
    Console.WriteLine("❌ No browsing support (e.g., YouTube Music)");

if (caps.SupportsAbsoluteVolume)
    Console.WriteLine("✅ Supports volume control");

Console.WriteLine($"Supported codecs: {string.Join(", ", caps.A2dp.SupportedCodecs.Select(c => c.Name))}");
```

## 🐛 Troubleshooting

### **Common Issues**

#### **"Device not found" errors**
```csharp
// Always validate MAC address format and check device capabilities first
if (!BlueZNet.Services.BlueZNetControllerService.IsValidMacAddress(deviceAddress))
{
    Console.WriteLine("Invalid MAC address format");
    return;
}

var device = (await controller.GetConnectedDevicesAsync()).FirstOrDefault(d => d.Name.Contains("MyPhone"));
if (device == null)
{
    Console.WriteLine("Device not connected");
    return;
}

var caps = await controller.GetDeviceCapabilitiesAsync(device.Address);
if (!caps.Avrcp.SupportsPlayback)
{
    Console.WriteLine("Device doesn't support media control");
    return;
}
```

#### **Media control not working**
1. **Check AVRCP support**: Some apps don't implement full AVRCP
2. **Verify BlueZ experimental features**: Required for advanced functionality
3. **Test with different apps**: Try with built-in music player vs streaming apps
4. **Use cancellation tokens**: Prevent operations from hanging indefinitely

#### **Audio processing not available**
```bash
# Install LADSPA plugins
sudo apt install ladspa-sdk swh-plugins cmt

# Verify installation
ls /usr/lib/ladspa/ | grep -E "(sc4|gate)"
```

#### **D-Bus connection errors**
```bash
# Check BlueZ service status
sudo systemctl status bluetooth

# Verify experimental features
sudo systemctl status bluetooth | grep experimental

# Check D-Bus permissions
sudo systemctl restart bluetooth
```

### **Performance Tips**

#### **Use cancellation tokens effectively**
```csharp
// Set reasonable timeouts for operations
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
try 
{
    await controller.PlayAsync(deviceAddress, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation timed out");
}
```

#### **Handle collection modifications safely**
```csharp
// The library handles this internally, but for your code:
var devices = await controller.GetConnectedDevicesAsync();
foreach (var device in devices.ToList()) // ToList() prevents modification exceptions
{
    // Process devices
}
```

### **Debugging Tips**

#### **Enable detailed logging**
```csharp
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
```

#### **Monitor D-Bus traffic**
```bash
# Watch BlueZ D-Bus messages
dbus-monitor --system "interface='org.bluez.*'"
```

#### **Test device capabilities**
```csharp
var device = devices.First();
var caps = await controller.GetDeviceCapabilitiesAsync(device.Address);

Console.WriteLine($"AVRCP Version: {caps.Avrcp.Version}");
Console.WriteLine($"Supported Commands: {string.Join(", ", caps.Avrcp.SupportedCommands)}");
Console.WriteLine($"Supports Browsing: {caps.SupportsBrowsing}");
Console.WriteLine($"Supports Volume: {caps.SupportsAbsoluteVolume}");
```

## 🧪 Testing

### **Unit Tests**
```bash
dotnet test
```

### **Integration Tests**
```bash
# Requires actual Bluetooth devices
dotnet test --category Integration
```

### **Manual Testing**
The library includes a comprehensive example application for manual testing:

```bash
cd BlueZNet.Example
dotnet run
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### **Development Setup**
1. **Clone the repository**
   ```bash
   git clone https://github.com/ByronAP/BlueZNet.git
   cd BlueZNet
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Set up test environment**
   - Raspberry Pi or Linux system with BlueZ
   - Bluetooth devices for testing
   - Follow the [setup guide](docs/setup.md)

4. **Run tests**
   ```bash
   dotnet test
   ```

### **Pull Request Process**
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests for new functionality
5. Update documentation
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **BlueZ project** - Linux Bluetooth stack
- **Tmds.DBus** - Excellent D-Bus library for .NET
- **PulseAudio** - Linux audio server
- **Raspberry Pi Foundation** - Making IoT development accessible

## 📚 Additional Resources

- **[Complete Setup Guide](docs/setup.md)** - Detailed BlueZ and PulseAudio configuration
- **[API Documentation](docs/api.md)** - Complete API reference
- **[Examples](examples/)** - Additional usage examples
- **[Troubleshooting Guide](docs/troubleshooting.md)** - Common issues and solutions

## 🔗 Related Projects

- **[BlueZ](http://www.bluez.org/)** - Official Linux Bluetooth protocol stack
- **[PulseAudio](https://www.freedesktop.org/wiki/Software/PulseAudio/)** - Advanced Linux sound server
- **[Tmds.DBus](https://github.com/tmds/Tmds.DBus)** - D-Bus library for .NET