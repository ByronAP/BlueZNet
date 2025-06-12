using BlueZNet.Models.Audio;
using System.Collections.Generic;

namespace BlueZNet.Models.Capabilities
{
    /// <summary>
    /// Contains information about A2DP audio capabilities and codecs.
    /// </summary>
    public class A2dpCapabilities
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="A2dpCapabilities"/> class.
        /// </summary>
        /// <param name="supportedCodecs">The list of audio codecs supported by the device.</param>
        /// <param name="activeCodec">The currently active audio codec being used for streaming.</param>
        /// <param name="maxBitrate">The maximum bitrate supported by the device in kbps.</param>
        /// <param name="supportsHighQuality">Whether the device supports high-quality audio codecs.</param>
        public A2dpCapabilities(
            IReadOnlyList<AudioCodec> supportedCodecs = null,
            AudioCodec activeCodec = null,
            uint maxBitrate = 0,
            bool supportsHighQuality = false)
        {
            SupportedCodecs = supportedCodecs ?? new List<AudioCodec>();
            ActiveCodec = activeCodec;
            MaxBitrate = maxBitrate;
            SupportsHighQuality = supportsHighQuality;
        }

        /// <summary>
        /// Gets the list of audio codecs supported by the device.
        /// </summary>
        public IReadOnlyList<AudioCodec> SupportedCodecs { get; }

        /// <summary>
        /// Gets the currently active audio codec being used for streaming.
        /// </summary>
        public AudioCodec ActiveCodec { get; }

        /// <summary>
        /// Gets the maximum bitrate supported by the device in kbps.
        /// </summary>
        public uint MaxBitrate { get; }

        /// <summary>
        /// Gets a value indicating whether the device supports high-quality audio codecs.
        /// </summary>
        /// <remarks>
        /// This is true if the device supports codecs like aptX, LDAC, or other high-quality formats.
        /// </remarks>
        public bool SupportsHighQuality { get; }
    }
}