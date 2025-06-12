# BlueZNet API Documentation

API reference for the BlueZNet Bluetooth control library.

## Table of Contents

- [Core Interfaces](#core-interfaces)
  - [IBlueZNetController](#iblueznetcontroller)
- [Events](#events)
  - [BluetoothConnectionEventArgs](#bluetoothconnectioneventargs)
  - [MediaPlaybackEventArgs](#mediaplaybackeventargs)
  - [AudioStreamEventArgs](#audiostreameventargs)
- [Data Models](#data-models)
  - [Device Models](#device-models)
  - [Media Models](#media-models)
  - [Audio Models](#audio-models)
  - [Capability Models](#capability-models)
- [Enums](#enums)
- [Exceptions](#exceptions)
- [DBus Interfaces](#dbus-interfaces)
- [Usage Examples](#usage-examples)

---

## Core Interfaces

### IBlueZNetController

The main interface for all Bluetooth operations. Provides comprehensive device monitoring, media control, and audio management capabilities.

```csharp
namespace BlueZNet.Interfaces
{
    public interface IBlueZNetController : IDisposable
```

#### Events

| Event | Type | Description |
|-------|------|-------------|
| `DeviceConnectionChanged` | `EventHandler<BluetoothConnectionEventArgs>` | Fired when a device connects or disconnects |
| `MediaPlaybackChanged` | `EventHandler<MediaPlaybackEventArgs>` | Fired when media playback state changes |
| `AudioStreamChanged` | `EventHandler<AudioStreamEventArgs>` | Fired when audio stream state changes |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsMonitoring` | `bool` | Indicates whether the controller is actively monitoring |

#### Monitoring Methods

##### StartMonitoringAsync
Starts monitoring Bluetooth devices and events.

```csharp
Task StartMonitoringAsync(CancellationToken cancellationToken = default)
```

**Parameters:**
- `cancellationToken`: Token to cancel the startup operation

**Exceptions:**
- `InvalidOperationException`: Monitoring is already active
- `BlueZNetException`: BlueZ or D-Bus connection failed

**Example:**
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await controller.StartMonitoringAsync(cts.Token);
```

##### StopMonitoringAsync
Stops monitoring and releases resources.

```csharp
Task StopMonitoringAsync()
```

**Example:**
```csharp
await controller.StopMonitoringAsync();
```

#### Device Query Methods

##### GetConnectedDevicesAsync
Gets currently connected Bluetooth devices.

```csharp
Task<IReadOnlyList<BluetoothDevice>> GetConnectedDevicesAsync(CancellationToken cancellationToken = default)
```

**Returns:** Read-only list of connected devices

**Example:**
```csharp
var devices = await controller.GetConnectedDevicesAsync();
foreach (var device in devices)
{
    Console.WriteLine($"{device.Name} - {device.Address}");
}
```

##### GetKnownDevicesAsync
Gets all known (paired and remembered) devices.

```csharp
Task<IReadOnlyList<BluetoothDevice>> GetKnownDevicesAsync(CancellationToken cancellationToken = default)
```

**Returns:** Read-only list of all known devices

##### GetAudioStreamInfoAsync
Gets audio stream information for a device.

```csharp
Task<AudioStreamInfo> GetAudioStreamInfoAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

**Parameters:**
- `deviceAddress`: MAC address of the device (format: XX:XX:XX:XX:XX:XX)

**Returns:** Audio stream info or null if no active stream

#### Media Control Methods

##### PlayAsync
Starts or resumes playback.

```csharp
Task<bool> PlayAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

**Parameters:**
- `deviceAddress`: MAC address of the device

**Returns:** `true` if successful, `false` otherwise

**Requirements:** Device must support AVRCP playback

##### PauseAsync
Pauses playback.

```csharp
Task<bool> PauseAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

##### StopAsync
Stops playback.

```csharp
Task<bool> StopAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

##### NextTrackAsync
Skips to the next track.

```csharp
Task<bool> NextTrackAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

##### PreviousTrackAsync
Goes to the previous track.

```csharp
Task<bool> PreviousTrackAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

##### FastForwardAsync
Fast forwards the current track.

```csharp
Task<bool> FastForwardAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

##### RewindAsync
Rewinds the current track.

```csharp
Task<bool> RewindAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

#### Position Control Methods

##### SeekAsync
Seeks to a specific position in the track.

```csharp
Task<bool> SeekAsync(string deviceAddress, uint positionMs, CancellationToken cancellationToken = default)
```

**Parameters:**
- `deviceAddress`: MAC address of the device
- `positionMs`: Position in milliseconds

**Example:**
```csharp
// Seek to 1 minute 30 seconds
await controller.SeekAsync(device.Address, 90000);
```

##### GetPositionAsync
Gets the current playback position.

```csharp
Task<uint?> GetPositionAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

**Returns:** Position in milliseconds or null if unavailable

#### Volume Control Methods

##### SetVolumeAsync
Sets the device volume.

```csharp
Task<bool> SetVolumeAsync(string deviceAddress, double volume, CancellationToken cancellationToken = default)
```

**Parameters:**
- `deviceAddress`: MAC address of the device
- `volume`: Volume level (0.0 to 1.0)

**Notes:** Attempts both AVRCP and PulseAudio volume control

**Example:**
```csharp
await controller.SetVolumeAsync(device.Address, 0.75); // 75% volume
```

##### SetMutedAsync
Sets the mute state.

```csharp
Task<bool> SetMutedAsync(string deviceAddress, bool muted, CancellationToken cancellationToken = default)
```

##### GetVolumeAsync
Gets the current volume level.

```csharp
Task<double?> GetVolumeAsync(string deviceAddress, CancellationToken cancellationToken = default)
```

**Returns:** Volume (0.0 to 1.0) or null if unavailable

#### Playback Mode Methods

##### SetShuffleAsync
Enables or disables shuffle mode.

```csharp
Task<bool> SetShuffleAsync(string deviceAddress, bool shuffle, CancellationToken cancellationToken = default)
```

##### SetRepeatModeAsync
Sets the repeat mode.

```csharp
Task<bool> SetRepeatModeAsync(string deviceAddress, string mode, CancellationToken cancellationToken = default)
```

**Parameters:**
- `mode`: One of: "off", "singletrack", "alltracks", "group"

#### Media Browsing Methods

##### BrowseMediaAsync
Browses available media folders.

```csharp
Task<IReadOnlyList<MediaFolder>> BrowseMediaAsync(
    string deviceAddress, 
    string folderPath = null, 
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `folderPath`: Path to browse or null for top-level

**Notes:** Not supported by all devices (e.g., YouTube Music)

##### GetFolderItemsAsync
Gets items within a media folder.

```csharp
Task<IReadOnlyList<MediaItem>> GetFolderItemsAsync(
    string deviceAddress, 
    string folderPath, 
    CancellationToken cancellationToken = default)
```

##### PlayItemAsync
Plays a specific media item.

```csharp
Task<bool> PlayItemAsync(
    string deviceAddress, 
    string itemPath, 
    CancellationToken cancellationToken = default)
```

##### AddToQueueAsync
Adds an item to the playback queue.

```csharp
Task<bool> AddToQueueAsync(
    string deviceAddress, 
    string itemPath, 
    CancellationToken cancellationToken = default)
```

##### GetCurrentPlaylistAsync
Gets current playlist information.

```csharp
Task<PlaylistInfo> GetCurrentPlaylistAsync(
    string deviceAddress, 
    CancellationToken cancellationToken = default)
```

#### Device Capability Methods

##### GetDeviceCapabilitiesAsync
Gets comprehensive device capabilities.

```csharp
Task<DeviceCapabilities> GetDeviceCapabilitiesAsync(
    string deviceAddress, 
    CancellationToken cancellationToken = default)
```

**Example:**
```csharp
var caps = await controller.GetDeviceCapabilitiesAsync(device.Address);
if (caps.SupportsBrowsing)
{
    var folders = await controller.BrowseMediaAsync(device.Address);
}
```

##### GetSupportedProfilesAsync
Gets supported Bluetooth profiles.

```csharp
Task<IReadOnlyList<AudioProfile>> GetSupportedProfilesAsync(
    string deviceAddress, 
    CancellationToken cancellationToken = default)
```

##### GetActiveProfileAsync
Gets the currently active profile.

```csharp
Task<AudioProfile?> GetActiveProfileAsync(
    string deviceAddress, 
    CancellationToken cancellationToken = default)
```

##### SwitchProfileAsync
Switches to a different Bluetooth profile.

```csharp
Task<bool> SwitchProfileAsync(
    string deviceAddress, 
    AudioProfile profile, 
    CancellationToken cancellationToken = default)
```

**Profiles:**
- `A2DP`: High-quality music streaming
- `HFP`: Hands-free calling with audio
- `HSP`: Basic headset functionality

##### IsFeatureSupportedAsync
Checks if a feature is supported.

```csharp
Task<bool> IsFeatureSupportedAsync(
    string deviceAddress, 
    string feature, 
    CancellationToken cancellationToken = default)
```

**Features:** "play", "pause", "volume", "browsing", "metadata", "seek", "next", "previous", "search"

#### Audio Processing Methods

##### SetDynamicRangeCompressionAsync
Configures audio processing settings.

```csharp
Task<bool> SetDynamicRangeCompressionAsync(
    string deviceAddress, 
    AudioProcessingSettings settings, 
    CancellationToken cancellationToken = default)
```

**Requirements:** LADSPA plugins must be installed

##### GetAudioProcessingSettingsAsync
Gets current audio processing settings.

```csharp
Task<AudioProcessingSettings> GetAudioProcessingSettingsAsync(
    string deviceAddress, 
    CancellationToken cancellationToken = default)
```

##### SetAudioCodecAsync
Attempts to set the audio codec.

```csharp
Task<bool> SetAudioCodecAsync(
    string deviceAddress, 
    string codecName, 
    CancellationToken cancellationToken = default)
```

**Codecs:** "SBC", "aptX", "AAC", "LDAC"

##### GetAudioCodecInfoAsync
Gets codec information.

```csharp
Task<A2dpCapabilities> GetAudioCodecInfoAsync(
    string deviceAddress, 
    CancellationToken cancellationToken = default)
```

---

## Events

### BluetoothConnectionEventArgs

Event data for device connection changes.

```csharp
namespace BlueZNet.Events
{
    public class BluetoothConnectionEventArgs : EventArgs
```

| Property | Type | Description |
|----------|------|-------------|
| `Device` | `BluetoothDevice` | The device that connected/disconnected |
| `IsConnected` | `bool` | True if connected, false if disconnected |
| `Timestamp` | `DateTime` | When the event occurred (UTC) |

### MediaPlaybackEventArgs

Event data for media playback changes.

```csharp
namespace BlueZNet.Events
{
    public class MediaPlaybackEventArgs : EventArgs
```

| Property | Type | Description |
|----------|------|-------------|
| `Device` | `BluetoothDevice` | The device whose media state changed |
| `MediaPlayer` | `MediaPlayerInfo` | Updated media player information |
| `Timestamp` | `DateTime` | When the event occurred (UTC) |

### AudioStreamEventArgs

Event data for audio stream changes.

```csharp
namespace BlueZNet.Events
{
    public class AudioStreamEventArgs : EventArgs
```

| Property | Type | Description |
|----------|------|-------------|
| `Device` | `BluetoothDevice` | The device whose audio stream changed |
| `AudioStream` | `AudioStreamInfo` | Updated audio stream information |
| `Timestamp` | `DateTime` | When the event occurred (UTC) |

---

## Data Models

### Device Models

#### BluetoothDevice

Complete device information.

```csharp
namespace BlueZNet.Models.Device
{
    public class BluetoothDevice
```

| Property | Type | Description |
|----------|------|-------------|
| `Address` | `string` | MAC address (XX:XX:XX:XX:XX:XX) |
| `Name` | `string` | Device friendly name |
| `ObjectPath` | `string` | BlueZ D-Bus object path |
| `Connected` | `bool` | Connection status |
| `LastSeen` | `DateTime` | Last update time |
| `MediaPlayer` | `MediaPlayerInfo` | Media player state (nullable) |
| `AudioStream` | `AudioStreamInfo` | Audio stream info (nullable) |
| `MediaBrowser` | `MediaBrowserInfo` | Media browsing info (nullable) |
| `Capabilities` | `DeviceCapabilities` | Device capabilities |
| `ActiveProfile` | `AudioProfile?` | Currently active profile |

**Methods:**
```csharp
BluetoothDevice WithUpdatedProperties(...)
```

### Media Models

#### MediaPlayerInfo

Current media player state.

```csharp
namespace BlueZNet.Models.Media
{
    public class MediaPlayerInfo
```

| Property | Type | Description |
|----------|------|-------------|
| `ObjectPath` | `string` | BlueZ D-Bus object path |
| `Status` | `string` | "playing", "paused", "stopped" |
| `Track` | `TrackMetadata` | Current track metadata (nullable) |
| `Position` | `uint?` | Position in milliseconds |
| `Shuffle` | `bool?` | Shuffle mode enabled |
| `Repeat` | `string` | Repeat mode |
| `Playlist` | `PlaylistInfo` | Playlist information (nullable) |

#### TrackMetadata

Track information.

```csharp
namespace BlueZNet.Models.Media
{
    public class TrackMetadata
```

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string` | Track title |
| `Artist` | `string` | Artist name |
| `Album` | `string` | Album name |
| `Genre` | `string` | Genre |
| `NumberOfTracks` | `uint?` | Total tracks in album |
| `TrackNumber` | `uint?` | Track number in album |
| `Duration` | `uint?` | Duration in milliseconds |

#### MediaFolder

Media library folder.

```csharp
namespace BlueZNet.Models.Media
{
    public class MediaFolder
```

| Property | Type | Description |
|----------|------|-------------|
| `ObjectPath` | `string` | BlueZ D-Bus object path |
| `Name` | `string` | Folder display name |
| `Type` | `string` | "mixed", "titles", "albums", "artists", "playlists" |
| `Playable` | `bool` | Can be played directly |
| `NumberOfItems` | `uint?` | Item count |

#### MediaItem

Individual media item.

```csharp
namespace BlueZNet.Models.Media
{
    public class MediaItem
```

| Property | Type | Description |
|----------|------|-------------|
| `ObjectPath` | `string` | BlueZ D-Bus object path |
| `Name` | `string` | Item display name |
| `Type` | `string` | "video", "audio", "folder" |
| `Playable` | `bool` | Can be played |
| `Metadata` | `TrackMetadata` | Track metadata (nullable) |

#### PlaylistInfo

Playlist information.

```csharp
namespace BlueZNet.Models.Media
{
    public class PlaylistInfo
```

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Playlist name |
| `TotalTracks` | `uint?` | Total track count |
| `CurrentTrackIndex` | `uint?` | Current track index (0-based) |

#### MediaBrowserInfo

Media browsing capabilities.

```csharp
namespace BlueZNet.Models.Media
{
    public class MediaBrowserInfo
```

| Property | Type | Description |
|----------|------|-------------|
| `ObjectPath` | `string` | BlueZ D-Bus object path |
| `BrowsingSupported` | `bool` | Browsing is supported |
| `AvailableFolders` | `IReadOnlyList<MediaFolder>` | Top-level folders |

### Audio Models

#### AudioStreamInfo

Audio stream information.

```csharp
namespace BlueZNet.Models.Audio
{
    public class AudioStreamInfo
```

| Property | Type | Description |
|----------|------|-------------|
| `SinkName` | `string` | PulseAudio sink name |
| `IsActive` | `bool` | Audio is actively playing |
| `Volume` | `double` | Volume level (0.0 to 1.0) |
| `IsMuted` | `bool` | Mute state |
| `Format` | `string` | Audio format (e.g., "s16le") |
| `SampleRate` | `uint?` | Sample rate in Hz |

#### AudioProcessingSettings

Audio processing configuration.

```csharp
namespace BlueZNet.Models.Audio
{
    public class AudioProcessingSettings
```

| Property | Type | Description |
|----------|------|-------------|
| `DynamicRangeCompressionEnabled` | `bool` | Enable compression |
| `CompressionRatio` | `double` | Compression ratio (e.g., 4.0) |
| `ThresholdDb` | `double` | Threshold in dB |
| `AttackMs` | `double` | Attack time in ms |
| `ReleaseMs` | `double` | Release time in ms |
| `NoiseGateEnabled` | `bool` | Enable noise gate |
| `NoiseGateThresholdDb` | `double` | Gate threshold in dB |

#### AudioCodec

Audio codec information.

```csharp
namespace BlueZNet.Models.Audio
{
    public class AudioCodec
```

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Codec name (SBC, aptX, etc.) |
| `Bitrate` | `uint` | Bitrate in kbps |
| `Quality` | `string` | "Low", "Standard", "High", "Lossless" |
| `IsActive` | `bool` | Currently active codec |

### Capability Models

#### DeviceCapabilities

Comprehensive device capabilities.

```csharp
namespace BlueZNet.Models.Capabilities
{
    public class DeviceCapabilities
```

| Property | Type | Description |
|----------|------|-------------|
| `Avrcp` | `AvrcpCapabilities` | AVRCP capabilities |
| `A2dp` | `A2dpCapabilities` | A2DP capabilities |
| `SupportedProfiles` | `IReadOnlyList<AudioProfile>` | Supported profiles |
| `SupportsAbsoluteVolume` | `bool` | AVRCP volume control |
| `SupportsBrowsing` | `bool` | Media browsing support |
| `SupportsSearch` | `bool` | Media search support |

#### AvrcpCapabilities

AVRCP-specific capabilities.

```csharp
namespace BlueZNet.Models.Capabilities
{
    public class AvrcpCapabilities
```

| Property | Type | Description |
|----------|------|-------------|
| `Version` | `string` | AVRCP version (e.g., "1.4") |
| `SupportedCommands` | `IReadOnlyList<string>` | Supported commands |
| `SupportsPlayback` | `bool` | Basic playback control |
| `SupportsVolumeControl` | `bool` | Volume control |
| `SupportsBrowsing` | `bool` | Media browsing |
| `SupportsMetadata` | `bool` | Track metadata |
| `SupportsPosition` | `bool` | Position/seek control |

#### A2dpCapabilities

A2DP-specific capabilities.

```csharp
namespace BlueZNet.Models.Capabilities
{
    public class A2dpCapabilities
```

| Property | Type | Description |
|----------|------|-------------|
| `SupportedCodecs` | `IReadOnlyList<AudioCodec>` | Available codecs |
| `ActiveCodec` | `AudioCodec` | Currently active codec |
| `MaxBitrate` | `uint` | Maximum bitrate in kbps |
| `SupportsHighQuality` | `bool` | High-quality codec support |

---

## Enums

### AudioProfile

Bluetooth audio profiles.

```csharp
namespace BlueZNet.Enums
{
    public enum AudioProfile
    {
        A2DP,   // Advanced Audio Distribution Profile
        HFP,    // Hands-Free Profile
        HSP,    // Headset Profile
        AVRCP   // Audio/Video Remote Control Profile
    }
}
```

### MediaControlResult

Media control operation results.

```csharp
namespace BlueZNet.Enums
{
    public enum MediaControlResult
    {
        Success,
        DeviceNotFound,
        DeviceNotConnected,
        MediaPlayerNotAvailable,
        CommandFailed,
        NotSupported
    }
}
```

---

## Exceptions

### BlueZNetException

Base exception for all BlueZNet errors.

```csharp
namespace BlueZNet.Exceptions
{
    public class BlueZNetException : Exception
    {
        public BlueZNetException();
        public BlueZNetException(string message);
        public BlueZNetException(string message, Exception innerException);
    }
}
```

**Common scenarios:**
- BlueZ service not running
- D-Bus connection failed
- Insufficient permissions
- Device not found

---

## DBus Interfaces

Low-level D-Bus interfaces used internally.

### IDevice
`org.bluez.Device1`

### IMediaPlayer
`org.bluez.MediaPlayer1`

### IMediaFolder
`org.bluez.MediaFolder1`

### IMediaItem
`org.bluez.MediaItem1`

### IObjectManager
`org.freedesktop.DBus.ObjectManager`

### IProperties
`org.freedesktop.DBus.Properties`

---

## Usage Examples

See the [README.md](../README.md) for detailed usage examples covering:
- Basic Monitoring
- Media Control
- Real-time Information
- Advanced Audio Processing
- Dependency Injection
- And more...