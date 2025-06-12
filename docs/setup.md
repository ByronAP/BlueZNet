# BlueZNet Setup Guide

This guide will walk you through setting up BlueZ and PulseAudio on Linux for use with the BlueZNet library. These instructions have been tested on Raspberry Pi OS, Ubuntu 20.04/22.04, and Debian 11/12.

## Table of Contents
- [Prerequisites](#prerequisites)
- [BlueZ Installation & Configuration](#bluez-installation--configuration)
- [PulseAudio Setup](#pulseaudio-setup)
- [D-Bus Configuration](#d-bus-configuration)
- [AVRCP Configuration](#avrcp-configuration)
- [Permissions & Security](#permissions--security)
- [Testing Your Setup](#testing-your-setup)
- [Troubleshooting](#troubleshooting)
- [Raspberry Pi Specific Setup](#raspberry-pi-specific-setup)

## Prerequisites

### System Requirements
- Linux-based operating system (kernel 4.4+)
- systemd-based init system
- .NET SDK/Runtime
- Bluetooth hardware (built-in or USB adapter)

### Required Packages
```bash
# Update package lists
sudo apt update

# Install core dependencies
sudo apt install -y \
    bluez \
    bluez-tools \
    pulseaudio \
    pulseaudio-module-bluetooth \
    libpulse-dev \
    dbus \
    libdbus-1-dev

# Optional but recommended
sudo apt install -y \
    bluez-firmware \
    bluetooth \
    libbluetooth-dev \
    python3-dbus
```

## BlueZ Installation & Configuration

### 1. Install BlueZ with Experimental Features

BlueZ experimental features are required for advanced AVRCP functionality including media browsing and absolute volume control.

#### Option A: Modify Service File (Recommended)
```bash
# Edit the bluetooth service
sudo systemctl edit --full bluetooth.service

# Find the ExecStart line and add --experimental flag:
# ExecStart=/usr/lib/bluetooth/bluetoothd --experimental

# Or use this one-liner:
sudo sed -i 's|ExecStart=/usr/lib/bluetooth/bluetoothd|ExecStart=/usr/lib/bluetooth/bluetoothd --experimental|g' /lib/systemd/system/bluetooth.service

# Reload systemd and restart bluetooth
sudo systemctl daemon-reload
sudo systemctl restart bluetooth
```

#### Option B: Create Override File
```bash
# Create override directory
sudo mkdir -p /etc/systemd/system/bluetooth.service.d

# Create override file
sudo tee /etc/systemd/system/bluetooth.service.d/01-experimental.conf > /dev/null <<EOF
[Service]
ExecStart=
ExecStart=/usr/lib/bluetooth/bluetoothd --experimental
EOF

# Reload and restart
sudo systemctl daemon-reload
sudo systemctl restart bluetooth
```

### 2. Configure BlueZ

Create or edit the main configuration file:

```bash
sudo nano /etc/bluetooth/main.conf
```

Add or modify these settings:

```ini
[General]
# Enable automatic reconnection
AutoConnect=true
AlwaysPairable=true

# Device class (0x000540 = Audio/Video)
Class=0x000540

# Enable all supported profiles
Enable=Source,Sink,Media,Socket

# Fast connectable for better reconnection
FastConnectable=true

[Policy]
# Auto-enable media control
AutoEnable=true

[AVRCP]
# Enable player and controller roles
MediaPlayerRole=true
RemoteControlRole=true

# Enable media browsing
MediaBrowserRole=true

# Volume sync
VolumeSync=true
```

### 3. Enable BR/EDR Mode

For audio streaming, ensure classic Bluetooth is enabled:

```bash
# Check current settings
sudo btmgmt info

# Enable BR/EDR if needed
sudo btmgmt bredr on
sudo btmgmt ssp on
```

## PulseAudio Setup

### 1. Install PulseAudio Bluetooth Support

```bash
# Install required modules
sudo apt install -y \
    pulseaudio-module-bluetooth \
    pulseaudio-utils \
    pavucontrol

# For LADSPA support (audio processing)
sudo apt install -y \
    ladspa-sdk \
    swh-plugins \
    cmt \
    tap-plugins
```

### 2. Configure PulseAudio for System Mode

For headless systems or services, system mode is recommended:

```bash
# Create system mode configuration
sudo tee /etc/pulse/system.pa > /dev/null <<'EOF'
# System mode PulseAudio configuration for BlueZNet

# Load core modules
load-module module-device-restore
load-module module-stream-restore
load-module module-card-restore
load-module module-augment-properties
load-module module-switch-on-port-available

# System mode specific
load-module module-native-protocol-unix auth-anonymous=1

# Bluetooth modules
load-module module-bluetooth-policy
load-module module-bluetooth-discover

# Network access (optional)
#load-module module-native-protocol-tcp auth-ip-acl=127.0.0.1;192.168.0.0/16

# Automatic routing
load-module module-always-sink
load-module module-intended-roles
load-module module-suspend-on-idle

# Enable position event handling
load-module module-position-event-sounds

# Cork music streams when phone call
load-module module-role-cork
EOF

# Create daemon configuration
sudo tee /etc/pulse/daemon.conf > /dev/null <<'EOF'
# PulseAudio daemon configuration for BlueZNet

# Run in system mode
system-instance = yes
daemonize = yes

# Performance settings
high-priority = yes
nice-level = -11
realtime-scheduling = yes
realtime-priority = 5

# Memory settings
cpu-limit = no
rlimit-rttime = 200000

# Audio settings
resample-method = speex-float-5
default-sample-format = s16le
default-sample-rate = 48000
alternate-sample-rate = 44100

# Bluetooth specific
bluetooth-discover-headset = yes

# Logging
log-level = notice
log-target = syslog
EOF
```

### 3. Create PulseAudio Service User

```bash
# Create pulse user if not exists
sudo useradd -r -s /bin/false -d /var/run/pulse pulse 2>/dev/null || true

# Add pulse user to required groups
sudo usermod -a -G audio,bluetooth pulse

# Add your application user to audio group
sudo usermod -a -G audio,pulse,bluetooth $USER
```

### 4. Enable System Mode Service

```bash
# Create systemd service for PulseAudio
sudo tee /etc/systemd/system/pulseaudio.service > /dev/null <<'EOF'
[Unit]
Description=PulseAudio system server
After=sound.target bluetooth.target

[Service]
Type=notify
ExecStart=/usr/bin/pulseaudio --system --realtime --disallow-exit --no-cpu-limit
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

# Enable and start the service
sudo systemctl daemon-reload
sudo systemctl enable pulseaudio.service
sudo systemctl start pulseaudio.service
```

## D-Bus Configuration

### 1. Configure D-Bus for Bluetooth Access

Create D-Bus policy for PulseAudio:

```bash
sudo tee /etc/dbus-1/system.d/pulseaudio-bluetooth.conf > /dev/null <<'EOF'
<!DOCTYPE busconfig PUBLIC
 "-//freedesktop//DTD D-BUS Bus Configuration 1.0//EN"
 "http://www.freedesktop.org/standards/dbus/1.0/busconfig.dtd">
<busconfig>
  <!-- Allow pulse user to own the service -->
  <policy user="pulse">
    <allow own="org.PulseAudio1"/>
    <allow own="org.bluez"/>
    <allow send_destination="org.bluez"/>
    <allow send_interface="org.bluez.MediaEndpoint1"/>
    <allow send_interface="org.bluez.MediaPlayer1"/>
    <allow send_interface="org.bluez.MediaControl1"/>
    <allow send_interface="org.bluez.MediaTransport1"/>
    <allow send_interface="org.bluez.Device1"/>
  </policy>

  <!-- Allow root to manage bluetooth -->
  <policy user="root">
    <allow own="org.bluez"/>
    <allow send_destination="org.bluez"/>
    <allow send_interface="org.bluez.*"/>
  </policy>

  <!-- Allow users in bluetooth group -->
  <policy group="bluetooth">
    <allow send_destination="org.bluez"/>
    <allow send_interface="org.bluez.MediaEndpoint1"/>
    <allow send_interface="org.bluez.MediaPlayer1"/>
    <allow send_interface="org.bluez.MediaControl1"/>
    <allow send_interface="org.bluez.Device1"/>
    <allow receive_sender="org.bluez"/>
  </policy>
</busconfig>
EOF
```

### 2. Application D-Bus Access

For your .NET application to access BlueZ:

```bash
# Create policy for your application
sudo tee /etc/dbus-1/system.d/blueznet-app.conf > /dev/null <<EOF
<!DOCTYPE busconfig PUBLIC
 "-//freedesktop//DTD D-BUS Bus Configuration 1.0//EN"
 "http://www.freedesktop.org/standards/dbus/1.0/busconfig.dtd">
<busconfig>
  <policy user="$USER">
    <allow send_destination="org.bluez"/>
    <allow send_interface="org.bluez.*"/>
    <allow send_interface="org.freedesktop.DBus.ObjectManager"/>
    <allow send_interface="org.freedesktop.DBus.Properties"/>
    <allow receive_sender="org.bluez"/>
  </policy>
</busconfig>
EOF

# Reload D-Bus configuration
sudo systemctl reload dbus
```

## AVRCP Configuration

### 1. Enable AVRCP Features

```bash
# Create AVRCP configuration
sudo mkdir -p /etc/bluetooth/avrcp
sudo tee /etc/bluetooth/avrcp/media.conf > /dev/null <<'EOF'
[General]
# Enable media player interface
Enable=true

# Support absolute volume
AbsoluteVolumeControl=true

# Enable track position synchronization
TrackPosition=true

# Media browsing support
MediaBrowsing=true

[Player]
# Supported features
Features=Play,Pause,Stop,Next,Previous,FastForward,Rewind,Seek,Volume,Shuffle,Repeat

# Metadata support
Metadata=Title,Artist,Album,Genre,NumberOfTracks,TrackNumber,Duration
EOF
```

### 2. Configure Audio Profiles

```bash
# Set up audio configuration
sudo tee /etc/bluetooth/audio.conf > /dev/null <<'EOF'
[General]
Enable=Source,Sink,Control
Disable=Headset

[A2DP]
SBCChannels=2
SBCBitpool=53
SBCMaxBitpool=53
SBCMinBitpool=2
SBCSubbands=8
SBCBlockLength=16
SBCAllocationMethod=Loudness

[AVRCP]
InputDeviceMode=2
EOF
```

## Permissions & Security

### 1. Set Correct Permissions

```bash
# Bluetooth daemon files
sudo chown -R root:bluetooth /etc/bluetooth
sudo chmod -R 755 /etc/bluetooth

# PulseAudio files
sudo chown -R pulse:pulse /var/run/pulse
sudo chmod 755 /var/run/pulse

# D-Bus policies
sudo chmod 644 /etc/dbus-1/system.d/*.conf
```

### 2. SELinux/AppArmor Considerations

If using SELinux:
```bash
# Check SELinux status
getenforce

# If enforcing, create policy for bluetooth
sudo setsebool -P bluetooth_helper_domain 1
```

If using AppArmor:
```bash
# Check AppArmor status
sudo aa-status

# If issues arise, temporarily disable for bluetooth
sudo ln -s /etc/apparmor.d/usr.sbin.bluetoothd /etc/apparmor.d/disable/
sudo apparmor_parser -R /etc/apparmor.d/usr.sbin.bluetoothd
```

## Testing Your Setup

### 1. Verify BlueZ Installation

```bash
# Check BlueZ version
bluetoothd -v

# Verify experimental features are enabled
sudo systemctl status bluetooth | grep -i experimental

# Check Bluetooth adapter
bluetoothctl show

# Test basic functionality
bluetoothctl
> power on
> scan on
> devices
> exit
```

### 2. Test PulseAudio

```bash
# Check PulseAudio is running
systemctl status pulseaudio

# List audio sinks
pactl list short sinks

# Check for bluetooth module
pactl list short modules | grep bluetooth

# Monitor real-time audio
pavucontrol  # GUI tool
```

### 3. Test Bluetooth Audio Connection

```bash
# Pair and connect a device
bluetoothctl
> power on
> discoverable on
> pairable on
> scan on
# Wait for your device to appear
> pair XX:XX:XX:XX:XX:XX
> trust XX:XX:XX:XX:XX:XX
> connect XX:XX:XX:XX:XX:XX
> exit

# Check audio connection
pactl list short sinks | grep bluez
```

### 4. Test with BlueZNet

Create a simple test application:

```csharp
using BlueZNet.Services;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
    
var controller = new BlueZNetControllerService(
    loggerFactory.CreateLogger<BlueZNetControllerService>());

controller.DeviceConnectionChanged += (s, e) =>
{
    Console.WriteLine($"Device {e.Device.Name} {(e.IsConnected ? "connected" : "disconnected")}");
};

await controller.StartMonitoringAsync();
Console.WriteLine("Monitoring started. Press any key to exit...");
Console.ReadKey();
await controller.StopMonitoringAsync();
```

## Troubleshooting

### Common Issues and Solutions

#### 1. "Failed to set mode: Blocked through rfkill"
```bash
# Unblock bluetooth
sudo rfkill unblock bluetooth
```

#### 2. "No default controller available"
```bash
# Check if bluetooth module is loaded
lsmod | grep bluetooth

# If not, load it
sudo modprobe bluetooth

# Check for hardware
dmesg | grep -i bluetooth
```

#### 3. "D-Bus connection failed"
```bash
# Check D-Bus is running
systemctl status dbus

# Test D-Bus access
dbus-send --system --dest=org.bluez --print-reply /org/bluez org.freedesktop.DBus.ObjectManager.GetManagedObjects
```

#### 4. "Audio not working"
```bash
# Check PulseAudio bluetooth connection
pactl list cards | grep -A 10 bluez

# Reset PulseAudio
pulseaudio -k
pulseaudio --start

# Or in system mode
sudo systemctl restart pulseaudio
```

#### 5. "AVRCP commands not working"
```bash
# Verify AVRCP profile is loaded
sdptool browse local | grep -A 5 "AVRCP"

# Check media player interface
dbus-send --system --dest=org.bluez --print-reply /org/bluez/hci0/dev_XX_XX_XX_XX_XX_XX org.freedesktop.DBus.Introspectable.Introspect | grep MediaPlayer
```

### Diagnostic Commands

```bash
# Full bluetooth diagnostics
sudo btmon &  # Run in background
bluetoothctl info XX:XX:XX:XX:XX:XX

# Check D-Bus objects
busctl tree org.bluez

# Monitor D-Bus messages
dbus-monitor --system "interface='org.bluez.MediaPlayer1'"

# Check system logs
journalctl -u bluetooth -f
journalctl -u pulseaudio -f
```

## Raspberry Pi Specific Setup

### Enable UART for Better Performance

```bash
# Edit config
sudo nano /boot/config.txt

# Add these lines
dtoverlay=disable-bt  # For USB Bluetooth adapters
# OR
dtoverlay=miniuart-bt  # For built-in Bluetooth

# Reboot
sudo reboot
```

### Optimize for Audio Streaming

```bash
# Increase USB throughput
echo "dwc_otg.speed=1" | sudo tee -a /boot/cmdline.txt

# Set CPU governor for consistent performance
echo "performance" | sudo tee /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor
```

### Memory Split for Headless Systems

```bash
# Reduce GPU memory
sudo raspi-config
# Advanced Options > Memory Split > 16
```

## Next Steps

1. **Test your setup** with the BlueZNet example application
2. **Configure logging** for your specific needs
3. **Set up monitoring** for production deployments
4. **Review security** settings for your environment
5. **Optimize performance** based on your hardware

For additional help:
- Check the [Troubleshooting Guide](troubleshooting.md)
- Review [API Documentation](api.md)
- See [Examples](../examples/) for more use cases