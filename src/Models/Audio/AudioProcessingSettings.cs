namespace BlueZNet.Models.Audio
{
    /// <summary>
    /// Settings for audio processing features like dynamic range compression.
    /// </summary>
    public class AudioProcessingSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioProcessingSettings"/> class.
        /// </summary>
        /// <param name="dynamicRangeCompressionEnabled">Whether dynamic range compression is enabled.</param>
        /// <param name="compressionRatio">The compression ratio.</param>
        /// <param name="thresholdDb">The threshold in dB above which compression is applied.</param>
        /// <param name="attackMs">The attack time in milliseconds.</param>
        /// <param name="releaseMs">The release time in milliseconds.</param>
        /// <param name="noiseGateEnabled">Whether noise gate is enabled.</param>
        /// <param name="noiseGateThresholdDb">The noise gate threshold in dB.</param>
        public AudioProcessingSettings(
            bool dynamicRangeCompressionEnabled = false,
            double compressionRatio = 1.0,
            double thresholdDb = -12.0,
            double attackMs = 5.0,
            double releaseMs = 100.0,
            bool noiseGateEnabled = false,
            double noiseGateThresholdDb = -60.0)
        {
            DynamicRangeCompressionEnabled = dynamicRangeCompressionEnabled;
            CompressionRatio = compressionRatio;
            ThresholdDb = thresholdDb;
            AttackMs = attackMs;
            ReleaseMs = releaseMs;
            NoiseGateEnabled = noiseGateEnabled;
            NoiseGateThresholdDb = noiseGateThresholdDb;
        }

        /// <summary>
        /// Gets a value indicating whether dynamic range compression is enabled.
        /// </summary>
        public bool DynamicRangeCompressionEnabled { get; }

        /// <summary>
        /// Gets the compression ratio (1.0 = no compression, higher values = more compression).
        /// </summary>
        /// <example>4.0 means 4:1 compression ratio</example>
        public double CompressionRatio { get; }

        /// <summary>
        /// Gets the threshold in dB above which compression is applied.
        /// </summary>
        public double ThresholdDb { get; }

        /// <summary>
        /// Gets the attack time in milliseconds (how quickly compression engages).
        /// </summary>
        public double AttackMs { get; }

        /// <summary>
        /// Gets the release time in milliseconds (how quickly compression disengages).
        /// </summary>
        public double ReleaseMs { get; }

        /// <summary>
        /// Gets a value indicating whether noise gate is enabled.
        /// </summary>
        public bool NoiseGateEnabled { get; }

        /// <summary>
        /// Gets the noise gate threshold in dB (signals below this are gated).
        /// </summary>
        public double NoiseGateThresholdDb { get; }
    }
}
