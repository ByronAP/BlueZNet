# BlueZNet Setup Guide

This guide will walk you through setting up BlueZ and PulseAudio on Linux for use with the BlueZNet library. These instructions have been tested on Raspberry Pi OS, Ubuntu 20.04/22.04, and Debian 11/12.

## Table of Contents
- [Prerequisites](#prerequisites)
- [BlueZ Installation & Configuration](#bluez-installation--configuration)
- [PulseAudio Setup](#pulseaudio-setup)
- [D-Bus Configuration](#d-bus-configuration)
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
    libbluetooth-dev
```

## BlueZ Installation & Configuration

### 1. Enable BlueZ Experimental Features

Experimental features are required for advanced AVRCP functionality including media browsing and absolute volume control.

#### Modify Service File (Recommended)
This method directly edits the systemd service file.

```bash
# Use sed to find and replace the ExecStart line
sudo sed -i 's|ExecStart=/usr/lib/bluetooth/bluetoothd.*|ExecStart=/usr/lib/bluetooth/bluetoothd --experimental|g' /lib/systemd/system/bluetooth.service

# Reload systemd and restart bluetooth
sudo systemctl daemon-reload
sudo systemctl restart bluetooth

# Verify the change
sudo systemctl status bluetooth | grep "ExecStart="
```

### 2. Configure BlueZ

Create or edit the main configuration file:

```bash
sudo nano /etc/bluetooth/main.conf
```

Ensure the following settings are present and uncommented:

```ini
[General]
# Enable automatic reconnection and pairing
AutoEnable=true
JustWorksRepairing=always
Class=0x00200414

[Policy]
# Auto-enable all profiles on connection
AutoEnable=true
```

Restart BlueZ after making changes: `sudo systemctl restart bluetooth`.

## PulseAudio Setup

### 1. Install PulseAudio Bluetooth Support

```bash
# Install required modules
sudo apt install -y \
    pulseaudio-module-bluetooth \
    pulseaudio-utils \
    pavucontrol

# For LADSPA support (audio processing features)
sudo apt install -y \
    ladspa-sdk \
    swh-plugins \
    cmt
```

### 2. Configure PulseAudio for System Mode

For headless systems or services that run without a user login, system mode is recommended.

```bash
# Create system mode configuration for PulseAudio
# This will load the necessary modules for Bluetooth audio
sudo tee /etc/pulse/system.pa > /dev/null <<'EOF'
# System mode PulseAudio configuration for BlueZNet

# Core modules
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

# Automatic routing and call handling
load-module module-intended-roles
load-module module-suspend-on-idle
load-module module-role-cork
EOF

# Create daemon configuration
sudo tee /etc/pulse/daemon.conf > /dev/null <<'EOF'
# PulseAudio daemon configuration for BlueZNet
system-instance = yes
daemonize = yes
high-priority = yes
nice-level = -11
realtime-scheduling = yes
realtime-priority = 5
resample-method = speex-float-5
default-sample-format = s16le
default-sample-rate = 44100
alternate-sample-rate = 48000
log-level = notice
log-target = syslog
EOF
```

### 3. Create PulseAudio Service User

```bash
# Add 'pulse' user to the 'bluetooth' group to allow it to interact with BlueZ
sudo useradd -g bluetooth pulse 2>/dev/null || sudo usermod -a -G bluetooth pulse

# Add your application's user to the 'pulse' and 'pulse-access' groups
sudo usermod -a -G pulse,pulse-access $USER
```

### 4. Enable System Mode Service

```bash
# Enable the system-wide PulseAudio service
sudo systemctl enable --now pulseaudio
```

## D-Bus Configuration

### 1. Configure D-Bus for Application Access

Your .NET application needs permission to communicate with BlueZ over D-Bus.

```bash
# Create a D-Bus policy file for your application's user
# Replace $USER with the actual user running the application if different
sudo tee /etc/dbus-1/system.d/blueznet-app.conf > /dev/null <<EOF
<!DOCTYPE busconfig PUBLIC
 "-//freedesktop//DTD D-BUS Bus Configuration 1.0//EN"
 "http://www.freedesktop.org/standards/dbus/1.0/busconfig.dtd">
<busconfig>
  <policy user="$USER">
    <allow send_destination="org.bluez"/>
    <allow receive_sender="org.bluez"/>
  </policy>
  <policy context="default">
    <deny send_destination="org.bluez"/>
  </policy>
</busconfig>
EOF

# Reload D-Bus configuration
sudo systemctl reload dbus
```

## Permissions & Security

### 1. Set Correct Permissions

```bash
# Ensure your user is in the 'bluetooth' group
sudo usermod -aG bluetooth $USER

# You may need to log out and log back in for group changes to take effect
```

### 2. AppArmor Considerations (Ubuntu)

If using AppArmor, you may need to adjust its policies if you encounter permission issues.

```bash
# Check AppArmor status
sudo aa-status

# If issues arise, you can temporarily disable the policy for bluetoothd
# This is NOT recommended for production environments
# sudo ln -s /etc/apparmor.d/usr.sbin.bluetoothd /etc/apparmor.d/disable/
# sudo apparmor_parser -R /etc/apparmor.d/usr.sbin.bluetoothd
```

## Testing Your Setup

### 1. Verify BlueZ Installation

```bash
# Check BlueZ version
bluetoothd --version

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

Create a simple test application (`TestApp.csproj` and `Program.cs`):

```csharp
// Program.cs
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

Run it: `dotnet run`

## Troubleshooting

See the dedicated [Troubleshooting Guide](troubleshooting.md) for detailed solutions to common problems.

## Raspberry Pi Specific Setup

### Enable UART for Better Performance

```bash
# Edit config
sudo nano /boot/config.txt

# For better performance with the onboard Bluetooth chip
# Add this line at the end:
dtoverlay=miniuart-bt

# Reboot
sudo reboot
```

### Optimize for Audio Streaming

```bash
# Set CPU governor for consistent performance
echo "performance" | sudo tee /sys/devices/system/cpu/cpu0/cpufreq/scaling_governor
```

### Memory Split for Headless Systems

```bash
# Reduce GPU memory to free up RAM for system processes
sudo raspi-config
# Select: Advanced Options > Memory Split > 16
```