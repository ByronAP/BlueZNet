using System;

namespace BlueZNet.Models.Audio
{
    /// <summary>
    /// Represents an audio codec with its characteristics and status.
    /// </summary>
    public class AudioCodec
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCodec"/> class.
        /// </summary>
        /// <param name="name">The name of the audio codec.</param>
        /// <param name="bitrate">The bitrate of the codec in kbps.</param>
        /// <param name="quality">The quality classification of the codec.</param>
        /// <param name="isActive">Whether this codec is currently active.</param>
        public AudioCodec(string name, uint bitrate = 0, string quality = "Standard", bool isActive = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Bitrate = bitrate;
            Quality = quality ?? "Standard";
            IsActive = isActive;
        }

        /// <summary>
        /// Gets the name of the audio codec.
        /// </summary>
        /// <example>SBC, aptX, AAC, LDAC</example>
        public string Name { get; }

        /// <summary>
        /// Gets the bitrate of the codec in kbps.
        /// </summary>
        public uint Bitrate { get; }

        /// <summary>
        /// Gets the quality classification of the codec.
        /// </summary>
        /// <example>Low, Standard, High, Lossless</example>
        public string Quality { get; }

        /// <summary>
        /// Gets a value indicating whether this codec is currently active.
        /// </summary>
        public bool IsActive { get; }

        public override bool Equals(object obj)
        {
            if (obj is AudioCodec other)
            {
                return Name == other.Name && Bitrate == other.Bitrate && Quality == other.Quality && IsActive == other.IsActive;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (Name?.GetHashCode() ?? 0) ^ Bitrate.GetHashCode() ^ (Quality?.GetHashCode() ?? 0) ^ IsActive.GetHashCode();
        }
    }
}
