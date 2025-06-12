using System;

namespace BlueZNet.Models.Audio
{
    /// <summary>
    /// Contains information about the current audio stream from PulseAudio.
    /// </summary>
    public class AudioStreamInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioStreamInfo"/> class.
        /// </summary>
        /// <param name="sinkName">The name of the PulseAudio sink handling this stream.</param>
        /// <param name="isActive">Whether audio is actively playing.</param>
        /// <param name="volume">The current volume level as a value between 0.0 and 1.0.</param>
        /// <param name="isMuted">Whether the audio is muted.</param>
        /// <param name="format">The audio format being used.</param>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        public AudioStreamInfo(string sinkName, bool isActive, double volume, bool isMuted, string format = null, uint? sampleRate = null)
        {
            SinkName = sinkName;
            IsActive = isActive;
            Volume = volume;
            IsMuted = isMuted;
            Format = format;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Gets the name of the PulseAudio sink handling this stream.
        /// </summary>
        public string SinkName { get; }

        /// <summary>
        /// Gets a value indicating whether audio is actively playing.
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Gets the current volume level as a value between 0.0 and 1.0.
        /// </summary>
        public double Volume { get; }

        /// <summary>
        /// Gets a value indicating whether the audio is muted.
        /// </summary>
        public bool IsMuted { get; }

        /// <summary>
        /// Gets the audio format being used.
        /// </summary>
        /// <example>s16le, s24le</example>
        public string Format { get; }

        /// <summary>
        /// Gets the sample rate in Hz.
        /// </summary>
        /// <example>44100, 48000</example>
        public uint? SampleRate { get; }

        public override bool Equals(object obj)
        {
            if (obj is AudioStreamInfo other)
            {
                return SinkName == other.SinkName && IsActive == other.IsActive &&
                       Math.Abs(Volume - other.Volume) < 0.001 && IsMuted == other.IsMuted &&
                       Format == other.Format && SampleRate == other.SampleRate;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (SinkName?.GetHashCode() ?? 0) ^ IsActive.GetHashCode() ^ Volume.GetHashCode() ^
                   IsMuted.GetHashCode() ^ (Format?.GetHashCode() ?? 0) ^ (SampleRate?.GetHashCode() ?? 0);
        }
    }
}
