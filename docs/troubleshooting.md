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
echo "--- BlueZ Status ---"
bluetoothd -v
sudo systemctl status bluetooth | grep -E "(Active:|ExecStart=)"
echo

# Check PulseAudio
echo "--- PulseAudio Status ---"
pulseaudio --version
systemctl --user status pulseaudio || systemctl status pulseaudio
echo

# Check Bluetooth hardware
echo "--- Bluetooth Hardware ---"
bluetoothctl show
echo

# Check D-Bus
echo "--- D-Bus Status ---"
systemctl status dbus
echo

# Check permissions
echo "--- User Groups ---"
groups $USER | grep -E "(audio|bluetooth|pulse)" || echo "User may be missing required groups."
echo

# Check for LADSPA plugins
echo "--- LADSPA Plugins ---"
ls /usr/lib/ladspa/ 2>/dev/null | grep -E "(sc4|gate)" || echo "LADSPA audio processing plugins not found."
```

---

## Installation Issues

### .NET Runtime Not Found

**Symptom:**
```
The framework 'Microsoft.NETCore.App', version '[x.x.x]' was not found.
```

**Solution:**
```bash
# Install .NET runtime
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
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
bluetoothd --version

# If not installed or old version
sudo apt update
sudo apt install bluez bluez-tools

# For latest version on older distros (e.g., Ubuntu LTS)
# sudo add-apt-repository ppa:bluetooth/bluez
# sudo apt update
# sudo apt upgrade bluez
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
sudo systemctl status bluetooth | grep experimental

# If not, enable it (see setup.md)
sudo sed -i 's|ExecStart=/usr/lib/bluetooth/bluetoothd.*|ExecStart=/usr/lib/bluetooth/bluetoothd --experimental|g' /lib/systemd/system/bluetooth.service
sudo systemctl daemon-reload
sudo systemctl restart bluetooth
```

#### 3. D-Bus Connection Failed
```bash
# Check D-Bus
systemctl status dbus

# Test D-Bus access
dbus-send --system --dest=org.bluez --print-reply / org.freedesktop.DBus.ObjectManager.GetManagedObjects

# If permission denied, check D-Bus policy files and user groups
ls -la /etc/dbus-1/system.d/*
groups $USER
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
sudo bluetoothctl power off
sudo bluetoothctl power on

# Check for hardware issues
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
# UUID: Audio Sink                (0000110b-...)
# UUID: A/V Remote Control        (0000110e-...)
```

#### 3. Media Player Not Ready
```csharp
// Wait for media player to be available after connection
controller.DeviceConnectionChanged += async (s, e) =>
{
    if (e.IsConnected)
    {
        await Task.Delay(2000); // Wait 2s for services to stabilize
        var caps = await controller.GetDeviceCapabilitiesAsync(e.Device.Address);
        if (caps.Avrcp.SupportsPlayback)
        {
            await controller.PlayAsync(e.Device.Address);
        }
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
}
```

#### 2. PulseAudio Sink Issues
```bash
# List Bluetooth sinks
pactl list short sinks | grep bluez

# Manually set volume
pactl set-sink-volume bluez_sink.XX_XX_XX_XX_XX_XX.a2dp_sink 50%

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
systemctl --user status pulseaudio || systemctl status pulseaudio

# Start if needed
systemctl --user start pulseaudio || sudo systemctl start pulseaudio
```

#### 2. Bluetooth Module Not Loaded
```bash
# Check modules
pactl list short modules | grep bluetooth

# Load if missing
pactl load-module module-bluetooth-discover
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

#### 1. Wi-Fi Interference
- If using a Raspberry Pi, try using the 5GHz Wi-Fi band, as 2.4GHz can interfere with Bluetooth.

#### 2. Adjust PulseAudio Latency
```bash
# Edit /etc/pulse/daemon.conf
default-fragments = 5
default-fragment-size-msec = 25
```
Restart PulseAudio after changes.

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
sudo apt install ladspa-sdk swh-plugins cmt

# Verify plugins
listplugins | grep -E "(sc4|gate)"

# Test manually
pactl load-module module-ladspa-sink \
  sink_name=compressed_sink master=bluez_sink.XX_XX_XX_XX_XX_XX.a2dp_sink \
  plugin=sc4_1882 label=sc4 control=1,1.5,401,-30,20,5,12
```

---

## Performance Issues

### High CPU Usage

**Common Causes:**

#### 1. Debug Logging Enabled
```csharp
// Use appropriate log level in production
.SetMinimumLevel(LogLevel.Information) // Not Debug or Trace
```

#### 2. Frequent Polling in App Logic
- Avoid tight loops calling `GetConnectedDevicesAsync` or other methods. Rely on the library's events instead.

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
   # 'trust' is crucial for iOS automatic reconnection
   bluetoothctl trust XX:XX:XX:XX:XX:XX
   ```

2. **Media control works intermittently**
   - iOS aggressively manages Bluetooth connections.
   - Ensure phone is unlocked during initial connection.
   - Keep the Music app in the foreground or background.

### Android Issues

**Common Problems:**

1. **YouTube Music doesn't support browsing**
   - This is expected behavior. Only playback control works.

2. **Some commands work, others don't**
   - Check Android Bluetooth settings for the device.
   - Enable "Media audio" and "Contact sharing".
   - Some Android skins limit AVRCP features.

---

## Error Messages

### Common Exceptions and Solutions

#### BlueZNetException
- Check BlueZ service is running.
- Verify experimental features are enabled.
- Check D-Bus permissions.

#### InvalidOperationException
- Ensure `StartMonitoringAsync` completed successfully before calling other methods.

#### Tmds.DBus.DBusException
- `org.bluez.Error.NotReady`: Device is not fully connected. Wait for `DeviceConnectionChanged` or add a delay.
- `org.bluez.Error.Failed`: A generic BlueZ error. Check `journalctl -u bluetooth` for details.
- `org.freedesktop.DBus.Error.AccessDenied`: Your user doesn't have permission. Check D-Bus policies and user groups.

---

## Debug Techniques

### Enable Detailed Logging

```csharp
// Maximum verbosity
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});
```

### Monitor D-Bus Traffic

```bash
# Monitor all BlueZ traffic
sudo dbus-monitor --system "interface='org.bluez.Device1'"

# Monitor media player messages for a specific device
sudo dbus-monitor --system "interface='org.bluez.MediaPlayer1',path='/org/bluez/hci0/dev_XX_XX_XX_XX_XX_XX/player0'"
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
# Monitor Bluetooth packets
sudo btmon
```

### PulseAudio Debugging

```bash
# Kill current PulseAudio
pulseaudio -k

# Run in verbose mode
pulseaudio -vvv
```

---

## Known Limitations

### Library Limitations

1. **No Bluetooth Classic Scanning**: Use `bluetoothctl` for device discovery. The library manages already-paired devices.
2. **No Direct Pairing Support**: Pair devices using `bluetoothctl` or a GUI.
3. **Limited Codec Control**: Codec switching requires reconnection and is not guaranteed to work on all devices.
4. **Single Instance per Process**: Only one `IBlueZNetController` should be active per application to avoid conflicts.

### Platform Limitations

1. **Raspberry Pi Bluetooth**: The built-in chip has limited bandwidth. Consider a USB Bluetooth adapter for high-quality audio or multiple connections.
2. **Virtual Machines**: Bluetooth passthrough is often problematic. Testing on physical hardware is recommended.
3. **WSL (Windows Subsystem for Linux)**: Does not have direct Bluetooth hardware access.

### Protocol Limitations

1. **AVRCP Versions**: Older devices may only support AVRCP 1.3 (no browsing, no position info).
2. **Media App Support**: Streaming apps may limit AVRCP features (e.g., browsing). DRM content may block metadata.