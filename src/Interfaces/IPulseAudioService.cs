using BlueZNet.Models.Audio;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Interfaces
{
    /// <summary>
    /// Handles all PulseAudio interactions for audio stream management.
    /// Implement this interface to customize audio system integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts PulseAudio operations, allowing you to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Customize audio stream detection and monitoring</description></item>
    /// <item><description>Implement alternative volume control mechanisms</description></item>
    /// <item><description>Add custom audio processing pipelines</description></item>
    /// <item><description>Mock audio operations for testing scenarios</description></item>
    /// <item><description>Integrate with different audio systems</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom implementation with caching
    /// public class CachingPulseAudioService : IPulseAudioService
    /// {
    ///     private readonly Dictionary&lt;string, AudioStreamInfo&gt; _cache = new();
    ///     
    ///     public async Task&lt;AudioStreamInfo&gt; GetAudioStreamInfoAsync(string deviceAddress, CancellationToken cancellationToken = default)
    ///     {
    ///         if (_cache.TryGetValue(deviceAddress, out var cached) && !IsStale(cached))
    ///             return cached;
    ///             
    ///         var info = await GetFreshAudioStreamInfoAsync(deviceAddress, cancellationToken);
    ///         _cache[deviceAddress] = info;
    ///         return info;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IPulseAudioService
    {
        /// <summary>
        /// Gets detailed audio stream information for a specific Bluetooth device.
        /// </summary>
        /// <param name="deviceAddress">The MAC address of the Bluetooth device.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Audio stream information, or null if no active stream is found.</returns>
        Task<AudioStreamInfo> GetAudioStreamInfoAsync(string deviceAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the volume for a specific PulseAudio sink.
        /// </summary>
        /// <param name="sinkName">The name of the PulseAudio sink.</param>
        /// <param name="volume">The volume level between 0.0 (silent) and 1.0 (maximum).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the volume was set successfully; otherwise, false.</returns>
        Task<bool> SetVolumeAsync(string sinkName, double volume, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the mute state for a specific PulseAudio sink.
        /// </summary>
        /// <param name="sinkName">The name of the PulseAudio sink.</param>
        /// <param name="muted">True to mute the sink; false to unmute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the mute state was set successfully; otherwise, false.</returns>
        Task<bool> SetMutedAsync(string sinkName, bool muted, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies dynamic range compression settings to a PulseAudio sink.
        /// </summary>
        /// <param name="sinkName">The name of the PulseAudio sink.</param>
        /// <param name="settings">The audio processing settings to apply.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>True if the settings were applied successfully; otherwise, false.</returns>
        Task<bool> SetDynamicRangeCompressionAsync(string sinkName, AudioProcessingSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current audio processing settings for a PulseAudio sink.
        /// </summary>
        /// <param name="sinkName">The name of the PulseAudio sink.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The current audio processing settings, or null if none are active.</returns>
        Task<AudioProcessingSettings> GetAudioProcessingSettingsAsync(string sinkName, CancellationToken cancellationToken = default);
    }
}