# BlueZNet Troubleshooting Guide

This guide helps resolve common issues when using the BlueZNet library with BlueZ and PulseAudio on Linux systems.

## Table of Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Installation Issues](#installation-issues)
- [Connection Problems](#connection-problems)
- [Media Control Issues](#media-control-issues)
- [Audio Stream Problems](#audio-stream-problems)
- [Performance Issues](#performance-issues)
- [Device-Specific Issues](#device-specific-issues)
- [Error Messages](#error-messages)
- [Debug Techniques](#debug-techniques)
- [Known Limitations](#known-limitations)

---

## Quick Diagnostics

Run this diagnostic script to check your system:

```bash
#!/bin/bash
echo "=== BlueZNet System Diagnostics ==="
echo

# Check BlueZ
echo "BlueZ Status:"
bluetoothd -v
sudo systemctl status bluetooth | grep -E "(Active:|experimental)"
echo

# Check PulseAudio
echo "PulseAudio Status:"
pulseaudio --version
systemctl status pulseaudio
echo

# Check Bluetooth hardware
echo "Bluetooth Hardware:"
hciconfig -a
echo

# Check D-Bus
echo "D-Bus Status:"
systemctl status dbus
echo

# Check permissions
echo "User Groups:"
groups $USER | grep -E "(audio|bluetooth|pulse)"
echo

# Check for LADSPA plugins
echo "LADSPA Plugins:"
ls /usr/lib/ladspa/ 2>/dev/null | grep -E "(sc4|gate)" || echo "No LADSPA plugins found"
```

---

## Installation Issues

### .NET Runtime Not Found

**Symptom:**
```
The framework 'Microsoft.NETCore.App', version '9.0.0' was not found.
```

**Solution:**
```bash
# Install .NET 9 runtime
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0
echo 'export PATH=$PATH:$HOME/.dotnet' >> ~/.bashrc
source ~/.bashrc

# Verify installation
dotnet --list-runtimes
```

### BlueZ Not Found or Wrong Version

**Symptom:**
```
bluetoothd: command not found
```

**Solution:**
```bash
# Check BlueZ version
bluetoothd -v

# If not installed or old version
sudo apt update
sudo apt install bluez bluez-tools

# For latest version on older distros
sudo add-apt-repository ppa:bluetooth/bluez
sudo apt update
sudo apt upgrade bluez
```

### Missing Dependencies

**Symptom:**
```
Could not load file or assembly 'Tmds.DBus'
```

**Solution:**
```bash
# In your project directory
dotnet restore
dotnet build

# If issues persist, clear NuGet cache
dotnet nuget locals all --clear
```

---

## Connection Problems

### "Failed to start Bluetooth monitoring"

**Symptom:**
```
BlueZNetException: Failed to start Bluetooth monitoring
```

**Common Causes & Solutions:**

#### 1. BlueZ Service Not Running
```bash
# Check status
sudo systemctl status bluetooth

# Start service
sudo systemctl start bluetooth
sudo systemctl enable bluetooth
```

#### 2. Experimental Features Not Enabled
```bash
# Check if experimental is enabled
ps aux | grep bluetoothd | grep experimental

# If not, enable it
sudo systemctl edit --full bluetooth.service
# Add --experimental to ExecStart line

sudo systemctl daemon-reload
sudo systemctl restart bluetooth
```

#### 3. D-Bus Connection Failed
```bash
# Check D-Bus
systemctl status dbus

# Test D-Bus access
dbus-send --system --dest=org.bluez --print-reply / org.freedesktop.DBus.ObjectManager.GetManagedObjects

# If permission denied, check policy files
ls -la /etc/dbus-1/system.d/*bluetooth*
```

### "No Bluetooth adapter found"

**Symptom:**
```
No default controller available
```

**Solutions:**

```bash
# Check if adapter is blocked
sudo rfkill list
sudo rfkill unblock bluetooth

# Check kernel modules
lsmod | grep bluetooth
sudo modprobe bluetooth

# Reset Bluetooth adapter
sudo hciconfig hci0 reset
sudo hciconfig hci0 up

# Check for firmware issues
dmesg | grep -i bluetooth
```

### Device Won't Connect

**Symptom:**
Device appears in scan but won't connect

**Solutions:**

```bash
# Remove and re-pair device
bluetoothctl
> remove XX:XX:XX:XX:XX:XX
> scan on
> pair XX:XX:XX:XX:XX:XX
> trust XX:XX:XX:XX:XX:XX
> connect XX:XX:XX:XX:XX:XX

# Clear BlueZ cache
sudo systemctl stop bluetooth
sudo rm -rf /var/lib/bluetooth/*/cache
sudo systemctl start bluetooth
```

---

## Media Control Issues

### Media Commands Not Working

**Symptom:**
```
PlayAsync returns false, no playback control
```

**Common Causes & Solutions:**

#### 1. App Doesn't Support AVRCP
```csharp
// Always check capabilities first
var caps = await controller.GetDeviceCapabilitiesAsync(deviceAddress);
if (!caps.Avrcp.SupportsPlayback)
{
    Console.WriteLine("Device/app doesn't support media control");
}
```

#### 2. Wrong Profile Active
```bash
# Check active profile
bluetoothctl info XX:XX:XX:XX:XX:XX | grep "UUID"

# Ensure A2DP and AVRCP are connected
# UUID: Audio Sink                (0000110b-0000-1000-8000-00805f9b34fb)
# UUID: A/V Remote Control        (0000110e-0000-1000-8000-00805f9b34fb)
```

#### 3. Media Player Not Ready
```csharp
// Wait for media player to be available
controller.MediaPlaybackChanged += async (s, e) =>
{
    if (e.MediaPlayer != null && e.MediaPlayer.Status != null)
    {
        // Now safe to send commands
        await controller.PlayAsync(e.Device.Address);
    }
};
```

### Volume Control Not Working

**Symptom:**
SetVolumeAsync has no effect

**Solutions:**

#### 1. Check Absolute Volume Support
```csharp
var caps = await controller.GetDeviceCapabilitiesAsync(deviceAddress);
if (!caps.SupportsAbsoluteVolume)
{
    Console.WriteLine("Device doesn't support AVRCP absolute volume");
    // Volume might only work via PulseAudio
}
```

#### 2. PulseAudio Sink Issues
```bash
# List Bluetooth sinks
pactl list short sinks | grep bluez

# Manually set volume
pactl set-sink-volume bluez_sink.XX_XX_XX_XX_XX_XX 50%

# Check if sink is suspended
pactl list sinks | grep -A 10 bluez
```

### Media Browsing Returns Empty

**Symptom:**
BrowseMediaAsync returns empty list

**Common Issues:**

1. **App doesn't support browsing** (YouTube Music, some streaming apps)
2. **Wrong player state** - Try playing something first
3. **Timing issue** - Wait after connection

```csharp
// Robust browsing with retry
public async Task<IReadOnlyList<MediaFolder>> BrowseWithRetryAsync(string deviceAddress)
{
    for (int i = 0; i < 3; i++)
    {
        var folders = await controller.BrowseMediaAsync(deviceAddress);
        if (folders.Any())
            return folders;
            
        await Task.Delay(2000); // Wait 2 seconds
    }
    
    return new List<MediaFolder>();
}
```

---

## Audio Stream Problems

### No Audio Stream Detected

**Symptom:**
GetAudioStreamInfoAsync returns null

**Solutions:**

#### 1. PulseAudio Not Running
```bash
# Check PulseAudio
systemctl --user status pulseaudio

# Or in system mode
systemctl status pulseaudio

# Start if needed
pulseaudio --start
```

#### 2. Bluetooth Module Not Loaded
```bash
# Check modules
pactl list short modules | grep bluetooth

# Load if missing
pactl load-module module-bluetooth-discover
pactl load-module module-bluetooth-policy
```

#### 3. Wrong Audio Profile
```csharp
// Ensure A2DP is active for audio streaming
var profile = await controller.GetActiveProfileAsync(deviceAddress);
if (profile != AudioProfile.A2DP)
{
    await controller.SwitchProfileAsync(deviceAddress, AudioProfile.A2DP);
}
```

### Audio Stuttering/Dropouts

**Solutions:**

#### 1. Increase Bluetooth MTU
```bash
# Edit /etc/bluetooth/main.conf
[LE]
MinConnectionInterval=6
MaxConnectionInterval=9
ConnectionLatency=0
ConnectionSupervisionTimeout=200
```

#### 2. Adjust PulseAudio Latency
```bash
# Edit /etc/pulse/daemon.conf
default-fragments = 8
default-fragment-size-msec = 10
```

#### 3. CPU Governor
```bash
# Set performance mode
echo performance | sudo tee /sys/devices/system/cpu/cpu*/cpufreq/scaling_governor
```

### Audio Processing Not Working

**Symptom:**
SetDynamicRangeCompressionAsync returns false

**Solutions:**

```bash
# Install LADSPA plugins
sudo apt install ladspa-sdk swh-plugins cmt tap-plugins

# Verify plugins
listplugins | grep -E "(sc4|gate)"

# Test manually
pactl load-module module-ladspa-sink \
  sink_name=compressed_sink \
  plugin=sc4_1882 \
  label=sc4 \
  control=1,1.5,401,-30,20,5,12
```

---

## Performance Issues

### High CPU Usage

**Common Causes:**

#### 1. Audio Stream Monitoring Too Frequent
```csharp
// The library polls every 5 seconds by default
// If you need to modify, fork and change:
private const int AudioStreamMonitorIntervalSeconds = 5; // Increase if needed
```

#### 2. Too Many Event Handlers
```csharp
// Ensure you unsubscribe when done
controller.MediaPlaybackChanged -= MyHandler;
```

#### 3. Debug Logging
```csharp
// Use appropriate log level in production
.SetMinimumLevel(LogLevel.Warning) // Not Debug
```

### Memory Leaks

**Prevention:**

```csharp
// Always dispose properly
using var controller = new BlueZNetControllerService(logger);
try
{
    await controller.StartMonitoringAsync();
    // ... use controller
}
finally
{
    await controller.StopMonitoringAsync();
}
```

### Slow Response Times

**Solutions:**

#### 1. Use Cancellation Tokens
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    await controller.PlayAsync(deviceAddress, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation timed out");
}
```

#### 2. Cache Device Capabilities
```csharp
private readonly Dictionary<string, DeviceCapabilities> _capabilitiesCache = new();

public async Task<DeviceCapabilities> GetCachedCapabilitiesAsync(string deviceAddress)
{
    if (!_capabilitiesCache.ContainsKey(deviceAddress))
    {
        _capabilitiesCache[deviceAddress] = 
            await controller.GetDeviceCapabilitiesAsync(deviceAddress);
    }
    return _capabilitiesCache[deviceAddress];
}
```

---

## Device-Specific Issues

### iPhone/iOS Issues

**Common Problems:**

1. **Won't connect after pairing**
   ```bash
   # Trust is crucial for iOS
   bluetoothctl trust XX:XX:XX:XX:XX:XX
   ```

2. **Media control works intermittently**
   - iOS aggressively manages Bluetooth connections
   - Ensure phone is unlocked during initial connection
   - Keep Music app in background

### Android Issues

**Common Problems:**

1. **YouTube Music doesn't support browsing**
   ```csharp
   // This is expected - YouTube Music doesn't implement browsing
   // Only playback control works
   ```

2. **Some commands work, others don't**
   - Check Android Bluetooth settings
   - Enable "Media audio" and "Contact sharing"
   - Some Android skins limit AVRCP features

### Bluetooth Speakers/Headphones

**When Pi connects TO speakers (role reversal):**

```bash
# Configure Pi as audio source
# Edit /etc/bluetooth/audio.conf
[General]
Enable=Source
```

---

## Error Messages

### Common Exceptions and Solutions

#### BlueZNetException
```
BlueZNetException: Failed to start Bluetooth monitoring
```
- Check BlueZ service is running
- Verify experimental features enabled
- Check D-Bus permissions

#### InvalidOperationException
```
InvalidOperationException: Connection not initialized
```
- Ensure StartMonitoringAsync completed successfully
- Don't call methods before monitoring starts

#### DBusException
```
DBusException: org.bluez.Error.NotReady
```
- Device not fully connected yet
- Wait for DeviceConnectionChanged event
- Add delay after connection

#### TimeoutException
```
The operation has timed out
```
- Use CancellationToken with reasonable timeout
- Check if device is in range
- Verify device isn't in power-saving mode

---

## Debug Techniques

### Enable Detailed Logging

```csharp
// Maximum verbosity
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .AddDebug()
        .SetMinimumLevel(LogLevel.Trace)
        .AddFilter("BlueZNet", LogLevel.Debug);
});
```

### Monitor D-Bus Traffic

```bash
# Monitor all BlueZ traffic
sudo dbus-monitor --system "interface='org.bluez.Device1'" | grep -A 5 -B 5 "member="

# Monitor specific device
sudo dbus-monitor --system "path='/org/bluez/hci0/dev_XX_XX_XX_XX_XX_XX'"
```

### BlueZ Debug Mode

```bash
# Enable BlueZ debug output
sudo systemctl stop bluetooth
sudo /usr/lib/bluetooth/bluetoothd -n -d &

# Watch logs
sudo journalctl -u bluetooth -f
```

### Packet-Level Debugging

```bash
# Install btmon
sudo apt install bluez-hcidump

# Monitor Bluetooth packets
sudo btmon

# In another terminal, reproduce the issue
```

### PulseAudio Debugging

```bash
# Kill current PulseAudio
pulseaudio -k

# Run in verbose mode
pulseaudio -vvv

# Check for errors during operations
```

### Create Minimal Test Case

```csharp
// Minimal test to isolate issues
class MinimalTest
{
    static async Task Main(string[] args)
    {
        var logger = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug))
            .CreateLogger<BlueZNetControllerService>();
            
        using var controller = new BlueZNetControllerService(logger);
        
        try
        {
            await controller.StartMonitoringAsync();
            Console.WriteLine("Started successfully");
            
            var devices = await controller.GetConnectedDevicesAsync();
            Console.WriteLine($"Found {devices.Count} devices");
            
            foreach (var device in devices)
            {
                Console.WriteLine($"- {device.Name} ({device.Address})");
                var caps = await controller.GetDeviceCapabilitiesAsync(device.Address);
                Console.WriteLine($"  AVRCP: {caps.Avrcp.Version}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
```

---

## Known Limitations

### Library Limitations

1. **No Bluetooth Classic Scanning**
   - Use bluetoothctl for device discovery
   - Library focuses on managing already-paired devices

2. **No Direct Pairing Support**
   - Pair devices using bluetoothctl or GUI
   - Library handles post-pairing operations

3. **Limited Codec Control**
   - Codec switching requires reconnection
   - Not all devices support codec selection

4. **Single Instance per Process**
   - One IBlueZNetController per application
   - Multiple instances may conflict

### Platform Limitations

1. **Raspberry Pi Bluetooth**
   - Built-in Bluetooth has limited bandwidth
   - Consider USB Bluetooth adapter for better performance

2. **Virtual Machines**
   - Bluetooth passthrough often problematic
   - Test on physical hardware

3. **WSL (Windows Subsystem for Linux)**
   - No Bluetooth support in WSL 1/2
   - Use native Linux for development

### Protocol Limitations

1. **AVRCP Versions**
   - Older devices may only support AVRCP 1.3
   - No browsing in AVRCP < 1.4
   - Position info requires AVRCP 1.3+

2. **Media App Support**
   - Streaming apps may limit AVRCP features
   - DRM content may block metadata
   - Some apps don't implement browsing

---

## Getting Help

### Before Asking for Help

1. **Run diagnostics script** (see top of this guide)
2. **Check all log outputs** with debug logging enabled
3. **Test with different devices** to isolate issue
4. **Create minimal reproduction** case
5. **Document your environment**:
   - Linux distribution and version
   - .NET version (`dotnet --info`)
   - BlueZ version (`bluetoothd -v`)
   - Device make/model
   - Relevant configuration files

### Where to Get Help

1. **GitHub Issues**: [https://github.com/ByronAP/BlueZNet/issues](https://github.com/ByronAP/BlueZNet/issues)
2. **Discussions**: [https://github.com/ByronAP/BlueZNet/discussions](https://github.com/ByronAP/BlueZNet/discussions)
3. **Stack Overflow**: Tag with `blueznet` and `bluetooth`

### Reporting Bugs

Include:
- Full error message and stack trace
- Minimal code to reproduce
- Debug logs
- System configuration
- What you expected vs what happened

Example bug report:
```markdown
**Environment:**
- OS: Raspberry Pi OS 11 (bullseye)
- .NET: 9.0.100
- BlueZ: 5.66
- Device: iPhone 13 (iOS 17.2)

**Code:**
```csharp
await controller.PlayAsync("XX:XX:XX:XX:XX:XX");
```

**Expected:** Music starts playing
**Actual:** Returns false, no playback

**Logs:**
[Attach debug logs]
```