using System;
using System.Linq;

namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Contains information about a BlueZ MediaEndpoint object.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MediaEndpoint objects represent the codec capabilities available for
    /// communication with Bluetooth audio devices. Each endpoint corresponds
    /// to a specific codec that can be negotiated during A2DP connection setup.
    /// </para>
    /// <para>
    /// Endpoints are typically created by BlueZ based on the local system's
    /// codec support and the remote device's advertised capabilities. They
    /// define the parameters that will be used for audio encoding/decoding.
    /// </para>
    /// </remarks>
    public class MediaEndpointInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaEndpointInfo"/> class.
        /// </summary>
        /// <param name="objectPath">The BlueZ D-Bus object path for this endpoint.</param>
        /// <param name="uuid">The A2DP service UUID for this endpoint.</param>
        /// <param name="codec">The codec identifier byte.</param>
        /// <param name="capabilities">The codec-specific capability bytes.</param>
        public MediaEndpointInfo(
            string objectPath,
            string uuid = null,
            byte codec = 0,
            byte[] capabilities = null)
        {
            ObjectPath = objectPath ?? throw new ArgumentNullException(nameof(objectPath));
            UUID = uuid;
            Codec = codec;
            Capabilities = capabilities;
        }

        /// <summary>
        /// Gets the BlueZ D-Bus object path for this endpoint.
        /// </summary>
        /// <example>/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF/ep0</example>
        public string ObjectPath { get; }

        /// <summary>
        /// Gets the A2DP service UUID for this endpoint.
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
        /// Standard A2DP codec identifiers:
        /// </para>
        /// <list type="bullet">
        /// <item><description>0x00 - SBC (Subband Coding) - Mandatory baseline codec</description></item>
        /// <item><description>0x01 - MPEG-1,2 Audio (MP3) - Optional</description></item>
        /// <item><description>0x02 - MPEG-2,4 AAC - Optional</description></item>
        /// </list>
        /// <para>
        /// Vendor-specific codec identifiers (require vendor ID in capabilities):
        /// </para>
        /// <list type="bullet">
        /// <item><description>0x40 - aptX (Qualcomm)</description></item>
        /// <item><description>0x41 - aptX HD (Qualcomm)</description></item>
        /// <item><description>0xAA - LDAC (Sony)</description></item>
        /// </list>
        /// </remarks>
        public byte Codec { get; }

        /// <summary>
        /// Gets the codec-specific capability bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The capability format is codec-dependent and defines the supported parameters:
        /// </para>
        /// <list type="bullet">
        /// <item><description>SBC: Supported sampling frequencies, channel modes, block lengths, subbands, allocation methods, and bitpool ranges</description></item>
        /// <item><description>AAC: Supported object types, sampling frequencies, channels, VBR support, and bitrate ranges</description></item>
        /// <item><description>aptX/LDAC: Vendor-specific capability structures</description></item>
        /// </list>
        /// <para>
        /// These capabilities are used during codec negotiation to find the best
        /// configuration that both devices support.
        /// </para>
        /// </remarks>
        public byte[] Capabilities { get; }

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
        /// Gets a value indicating whether this endpoint represents a high-quality codec.
        /// </summary>
        /// <remarks>
        /// High-quality codecs include AAC, aptX, aptX HD, and LDAC.
        /// SBC and MP3 are considered standard quality.
        /// </remarks>
        public bool IsHighQuality
        {
            get
            {
                switch (Codec)
                {
                    case 0x02: // AAC
                    case 0x40: // aptX
                    case 0x41: // aptX HD
                    case 0xAA: // LDAC
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this endpoint represents a lossless or near-lossless codec.
        /// </summary>
        /// <remarks>
        /// Currently, aptX HD and LDAC are considered lossless or near-lossless codecs.
        /// </remarks>
        public bool IsLossless
        {
            get
            {
                switch (Codec)
                {
                    case 0x41: // aptX HD
                    case 0xAA: // LDAC
                        return true;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this endpoint uses a vendor-specific codec.
        /// </summary>
        /// <remarks>
        /// Vendor-specific codecs require additional vendor identification in the capabilities.
        /// </remarks>
        public bool IsVendorSpecific => Codec >= 0x40;

        /// <summary>
        /// Gets the estimated maximum bitrate for this codec in kbps.
        /// </summary>
        /// <remarks>
        /// This provides a rough estimate based on typical codec capabilities.
        /// Actual bitrate depends on the negotiated configuration parameters.
        /// </remarks>
        public uint EstimatedMaxBitrate
        {
            get
            {
                switch (Codec)
                {
                    case 0x00: return 328;   // SBC (at high quality settings)
                    case 0x01: return 320;   // MP3
                    case 0x02: return 320;   // AAC
                    case 0x40: return 352;   // aptX
                    case 0x41: return 576;   // aptX HD
                    case 0xAA: return 990;   // LDAC (at highest quality setting)
                    default: return 0;
                }
            }
        }

        /// <summary>
        /// Returns a string representation of the endpoint information.
        /// </summary>
        /// <returns>A formatted string containing key endpoint details.</returns>
        public override string ToString()
        {
            var qualityDescription = IsLossless ? "Lossless" : IsHighQuality ? "High Quality" : "Standard";
            var capabilitiesInfo = Capabilities?.Length > 0 ? $" ({Capabilities.Length} capability bytes)" : "";
            return $"MediaEndpoint: {CodecName} ({qualityDescription}){capabilitiesInfo}";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current endpoint info.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>True if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is MediaEndpointInfo other)
            {
                return ObjectPath == other.ObjectPath &&
                       UUID == other.UUID &&
                       Codec == other.Codec &&
                       (Capabilities?.SequenceEqual(other.Capabilities ?? new byte[0]) ?? other.Capabilities == null);
            }
            return false;
        }

        /// <summary>
        /// Returns a hash code for the current endpoint info.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (ObjectPath?.GetHashCode() ?? 0);
                hash = hash * 23 + (UUID?.GetHashCode() ?? 0);
                hash = hash * 23 + Codec.GetHashCode();

                if (Capabilities != null)
                {
                    foreach (byte b in Capabilities)
                    {
                        hash = hash * 23 + b.GetHashCode();
                    }
                }

                return hash;
            }
        }
    }
}