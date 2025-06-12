using System;
using System.Linq;

namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Contains information about a BlueZ MediaTransport object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MediaTransport objects represent the communication channels between BlueZ
    /// and audio devices using the A2DP (Advanced Audio Distribution Profile).
    /// Each transport is associated with a specific codec and device.
    /// </para>
    /// <para>
    /// Transport states indicate the current status:
    /// </para>
    /// <list type="bullet">
    /// <item><description>"idle" - Transport is available but not in use</description></item>
    /// <item><description>"pending" - Transport is being configured or acquired</description></item>
    /// <item><description>"active" - Transport is actively streaming audio</description></item>
    /// </list>
    /// </remarks>
    public class MediaTransportInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTransportInfo"/> class.
        /// </summary>
        /// <param name="objectPath">The BlueZ D-Bus object path for this transport.</param>
        /// <param name="device">The object path of the associated Bluetooth device.</param>
        /// <param name="uuid">The A2DP service UUID for this transport.</param>
        /// <param name="codec">The codec identifier byte.</param>
        /// <param name="state">The current transport state.</param>
        /// <param name="volume">The absolute volume level (0-127), if supported.</param>
        /// <param name="configuration">The codec-specific configuration bytes.</param>
        public MediaTransportInfo(
            string objectPath,
            string device = null,
            string uuid = null,
            byte codec = 0,
            string state = "idle",
            ushort volume = 0,
            byte[] configuration = null)
        {
            ObjectPath = objectPath ?? throw new ArgumentNullException(nameof(objectPath));
            Device = device;
            UUID = uuid;
            Codec = codec;
            State = state ?? "idle";
            Volume = volume;
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the BlueZ D-Bus object path for this transport.
        /// </summary>
        /// <example>/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF/fd0</example>
        public string ObjectPath { get; }

        /// <summary>
        /// Gets the object path of the associated Bluetooth device.
        /// </summary>
        /// <example>/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF</example>
        public string Device { get; }

        /// <summary>
        /// Gets the A2DP service UUID for this transport.
        /// </summary>
        /// <remarks>
        /// This is typically "0000110a-0000-1000-8000-00805f9b34fb" for A2DP Sink
        /// or "0000110b-0000-1000-8000-00805f9b34fb" for A2DP Source.
        /// </remarks>
        public string UUID { get; }

        /// <summary>
        /// Gets the codec identifier byte.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Common codec identifiers:
        /// </para>
        /// <list type="bullet">
        /// <item><description>0x00 - SBC (Subband Coding)</description></item>
        /// <item><description>0x01 - MPEG-1,2 Audio (MP3)</description></item>
        /// <item><description>0x02 - MPEG-2,4 AAC</description></item>
        /// <item><description>0x40 - aptX (vendor specific)</description></item>
        /// <item><description>0x41 - aptX HD (vendor specific)</description></item>
        /// <item><description>0xAA - LDAC (vendor specific)</description></item>
        /// </list>
        /// </remarks>
        public byte Codec { get; }

        /// <summary>
        /// Gets the current transport state.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Possible states:
        /// </para>
        /// <list type="bullet">
        /// <item><description>"idle" - Transport is available but not streaming</description></item>
        /// <item><description>"pending" - Transport is being acquired or configured</description></item>
        /// <item><description>"active" - Transport is actively streaming audio data</description></item>
        /// </list>
        /// </remarks>
        public string State { get; }

        /// <summary>
        /// Gets the absolute volume level (0-127), if supported by the device.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property is only meaningful for devices that support AVRCP absolute volume control.
        /// A value of 0 indicates that volume information is not available or not applicable.
        /// </para>
        /// <para>
        /// The volume range is 0-127 where:
        /// </para>
        /// <list type="bullet">
        /// <item><description>0 = Muted or volume info unavailable</description></item>
        /// <item><description>127 = Maximum volume</description></item>
        /// </list>
        /// </remarks>
        public ushort Volume { get; }

        /// <summary>
        /// Gets the codec-specific configuration bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The configuration format depends on the codec:
        /// </para>
        /// <list type="bullet">
        /// <item><description>SBC: 4 bytes containing sampling frequency, channel mode, block length, subbands, allocation method, and bitpool values</description></item>
        /// <item><description>AAC: 6+ bytes containing object type, sampling frequency, channels, VBR flag, and bitrate</description></item>
        /// <item><description>aptX: Vendor-specific configuration</description></item>
        /// <item><description>LDAC: Vendor-specific configuration with quality mode</description></item>
        /// </list>
        /// <para>
        /// This data is used to configure the audio encoder/decoder pipeline.
        /// </para>
        /// </remarks>
        public byte[] Configuration { get; }

        /// <summary>
        /// Gets a value indicating whether this transport is currently active.
        /// </summary>
        public bool IsActive => State == "active";

        /// <summary>
        /// Gets a value indicating whether this transport is currently pending.
        /// </summary>
        public bool IsPending => State == "pending";

        /// <summary>
        /// Gets a value indicating whether this transport is idle.
        /// </summary>
        public bool IsIdle => State == "idle";

        /// <summary>
        /// Gets a human-readable string representation of the codec.
        /// </summary>
        public string CodecName
        {
            get
            {
                switch (Codec)
                {
                    case 0x00: return "SBC";
                    case 0x01: return "MP3";
                    case 0x02: return "AAC";
                    case 0x40: return "aptX";
                    case 0x41: return "aptX HD";
                    case 0xAA: return "LDAC";
                    default: return $"Unknown (0x{Codec:X2})";
                }
            }
        }

        /// <summary>
        /// Returns a string representation of the transport information.
        /// </summary>
        /// <returns>A formatted string containing key transport details.</returns>
        public override string ToString()
        {
            return $"MediaTransport: {CodecName} ({State}) - Volume: {Volume}/127";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current transport info.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            var other = obj as MediaTransportInfo;
            if (other == null)
            {
                return false;
            }

            // Compare primitive and string properties first for quick exit
            if (ObjectPath != other.ObjectPath ||
                Device != other.Device ||
                UUID != other.UUID ||
                Codec != other.Codec ||
                State != other.State ||
                Volume != other.Volume)
            {
                return false;
            }

            // Compare byte arrays
            if (Configuration == null && other.Configuration == null)
            {
                return true;
            }
            if (Configuration == null || other.Configuration == null)
            {
                return false; // One is null, the other is not
            }
            return Configuration.SequenceEqual(other.Configuration);
        }


        /// <summary>
        /// Returns a hash code for the current transport info.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (ObjectPath?.GetHashCode() ?? 0);
                hash = hash * 23 + (Device?.GetHashCode() ?? 0);
                hash = hash * 23 + (UUID?.GetHashCode() ?? 0);
                hash = hash * 23 + Codec.GetHashCode();
                hash = hash * 23 + (State?.GetHashCode() ?? 0);
                hash = hash * 23 + Volume.GetHashCode();

                if (Configuration != null)
                {
                    foreach (byte b in Configuration)
                    {
                        hash = hash * 23 + b.GetHashCode();
                    }
                }

                return hash;
            }
        }
    }
}