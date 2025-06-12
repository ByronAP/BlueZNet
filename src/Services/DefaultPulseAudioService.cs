using BlueZNet.Interfaces;
using BlueZNet.Models.Audio;
using BlueZNet.Models.Config;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BlueZNet.Services
{
    /// <summary>
    /// Default implementation of PulseAudio service for audio stream management.
    /// </summary>
    public class DefaultPulseAudioService : IPulseAudioService
    {
        private static readonly Regex MacAddressRegex = new Regex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", RegexOptions.Compiled);
        private static readonly Regex VolumeRegex = new Regex(@"(\d+)%", RegexOptions.Compiled);

        private readonly ILogger _logger;
        private readonly IProcessRunner _processRunner;
        private readonly BlueZNetConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPulseAudioService"/> class.
        /// </summary>
        /// <param name="processRunner">The process runner for executing pactl commands.</param>
        /// <param name="logger">The logger instance.</param>
        public DefaultPulseAudioService(ILogger logger = null)
            : this(new DefaultProcessRunner(logger), logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance with custom process runner.
        /// </summary>
        public DefaultPulseAudioService(IProcessRunner processRunner, ILogger logger = null)
            : this(processRunner, logger, BlueZNetConfiguration.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance with full configuration.
        /// </summary>
        public DefaultPulseAudioService(IProcessRunner processRunner, ILogger logger, BlueZNetConfiguration configuration)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            _configuration = configuration ?? BlueZNetConfiguration.Default;
        }

        /// <inheritdoc />
        public async Task<AudioStreamInfo> GetAudioStreamInfoAsync(string deviceAddress, CancellationToken cancellationToken = default)
        {
            if (!IsValidMacAddress(deviceAddress))
            {
                _logger.LogWarning("Invalid MAC address format: {DeviceAddress}", deviceAddress);
                return null;
            }

            try
            {
                if (!await _processRunner.CommandExistsAsync("pactl"))
                {
                    _logger.LogWarning("pactl command not found. PulseAudio may not be installed.");
                    return null;
                }

                // Query PulseAudio for active sinks related to the Bluetooth device
                var result = await _processRunner.RunAsync("pactl", "list short sinks", cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning("pactl list short sinks failed: {Error}", result.StandardError);
                    return null;
                }

                // Look for Bluetooth sink matching our device
                var lines = result.StandardOutput.Split('\n');
                foreach (var line in lines.Where(l => !string.IsNullOrEmpty(l)))
                {
                    if (line.Contains("bluez") && line.Contains(deviceAddress.Replace(":", "_")))
                    {
                        var parts = line.Split('\t');
                        if (parts.Length >= 2)
                        {
                            var sinkName = parts[1];
                            return await GetDetailedSinkInfoAsync(sinkName, cancellationToken);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audio stream info for {DeviceAddress}", deviceAddress);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetVolumeAsync(string sinkName, double volume, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sinkName))
                return false;

            volume = Math.Max(0.0, Math.Min(1.0, volume)); // Clamp between 0.0 and 1.0

            try
            {
                if (!await _processRunner.CommandExistsAsync("pactl"))
                    return false;

                var volumePercent = (int)(volume * 100);
                var result = await _processRunner.RunAsync("pactl", $"set-sink-volume {sinkName} {volumePercent}%", cancellationToken);

                if (result.Success)
                {
                    _logger.LogDebug("Set volume to {Volume}% for sink {SinkName}", volumePercent, sinkName);
                }
                else
                {
                    _logger.LogWarning("Failed to set volume for sink {SinkName}: {Error}", sinkName, result.StandardError);
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set volume for sink {SinkName}", sinkName);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetMutedAsync(string sinkName, bool muted, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sinkName))
                return false;

            try
            {
                if (!await _processRunner.CommandExistsAsync("pactl"))
                    return false;

                var muteArg = muted ? "1" : "0";
                var result = await _processRunner.RunAsync("pactl", $"set-sink-mute {sinkName} {muteArg}", cancellationToken);

                if (result.Success)
                {
                    _logger.LogDebug("Set mute {Muted} for sink {SinkName}", muted, sinkName);
                }
                else
                {
                    _logger.LogWarning("Failed to set mute for sink {SinkName}: {Error}", sinkName, result.StandardError);
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set mute for sink {SinkName}", sinkName);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SetDynamicRangeCompressionAsync(string sinkName, AudioProcessingSettings settings, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sinkName) || settings == null)
                return false;

            try
            {
                if (!await _processRunner.CommandExistsAsync("pactl"))
                    return false;

                var success = true;

                if (settings.DynamicRangeCompressionEnabled)
                {
                    // Load compressor module with settings
                    var compressorArgs = $"sink_name={sinkName}_compressed " +
                                       $"master={sinkName} " +
                                       $"ratio={settings.CompressionRatio} " +
                                       $"threshold={settings.ThresholdDb} " +
                                       $"attack={settings.AttackMs} " +
                                       $"release={settings.ReleaseMs}";

                    var command = $"load-module module-ladspa-sink plugin=sc4_1882 label=sc4 control={compressorArgs}";
                    var result = await _processRunner.RunAsync("pactl", command, cancellationToken);

                    if (!result.Success)
                    {
                        _logger.LogWarning("Failed to load compressor module: {Error}", result.StandardError);
                        success = false;
                    }
                }

                if (settings.NoiseGateEnabled)
                {
                    // Load noise gate module
                    var gateArgs = $"sink_name={sinkName}_gated " +
                                  $"master={sinkName} " +
                                  $"threshold={settings.NoiseGateThresholdDb}";

                    var command = $"load-module module-ladspa-sink plugin=gate_1410 label=gate control={gateArgs}";
                    var result = await _processRunner.RunAsync("pactl", command, cancellationToken);

                    if (!result.Success)
                    {
                        _logger.LogWarning("Failed to load noise gate module: {Error}", result.StandardError);
                        success = false;
                    }
                }

                if (success)
                {
                    _logger.LogInformation("Applied audio processing settings to sink {SinkName}", sinkName);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set dynamic range compression for sink {SinkName}", sinkName);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<AudioProcessingSettings> GetAudioProcessingSettingsAsync(string sinkName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!await _processRunner.CommandExistsAsync("pactl"))
                    return null;

                var result = await _processRunner.RunAsync("pactl", "list modules", cancellationToken);

                if (!result.Success)
                    return null;

                // Parse module list to detect active audio processing
                var output = result.StandardOutput;
                var modules = ParseModules(output);

                // Look for LADSPA modules related to our sink
                bool dynamicRangeCompressionEnabled = false;
                double compressionRatio = 1.0;
                double thresholdDb = -12.0;
                double attackMs = 5.0;
                double releaseMs = 100.0;
                bool noiseGateEnabled = false;
                double noiseGateThresholdDb = -60.0;

                foreach (var module in modules)
                {
                    if (module.Name == "module-ladspa-sink" && module.Arguments.ContainsKey("master"))
                    {
                        var masterSink = module.Arguments["master"];

                        // Check if this module is processing our sink or a related sink
                        if (masterSink == sinkName || masterSink.Contains(sinkName) || sinkName.Contains(masterSink))
                        {
                            // Check for compressor (sc4)
                            if (module.Arguments.ContainsKey("plugin") &&
                                module.Arguments["plugin"].Contains("sc4") &&
                                module.Arguments.ContainsKey("control"))
                            {
                                dynamicRangeCompressionEnabled = true;
                                var controlValues = ParseControlValues(module.Arguments["control"]);

                                if (controlValues.Length >= 4)
                                {
                                    if (double.TryParse(controlValues[0], out var ratio))
                                        compressionRatio = ratio;
                                    if (double.TryParse(controlValues[1], out var threshold))
                                        thresholdDb = threshold;
                                    if (double.TryParse(controlValues[2], out var attack))
                                        attackMs = attack;
                                    if (double.TryParse(controlValues[3], out var release))
                                        releaseMs = release;
                                }

                                _logger.LogDebug("Found compressor module for sink {SinkName}: ratio={Ratio}, threshold={Threshold}dB, attack={Attack}ms, release={Release}ms",
                                    sinkName, compressionRatio, thresholdDb, attackMs, releaseMs);
                            }

                            // Check for noise gate
                            if (module.Arguments.ContainsKey("plugin") &&
                                module.Arguments["plugin"].Contains("gate") &&
                                module.Arguments.ContainsKey("control"))
                            {
                                noiseGateEnabled = true;
                                var controlValues = ParseControlValues(module.Arguments["control"]);

                                if (controlValues.Length >= 1)
                                {
                                    if (double.TryParse(controlValues[0], out var gateThreshold))
                                        noiseGateThresholdDb = gateThreshold;
                                }

                                _logger.LogDebug("Found noise gate module for sink {SinkName}: threshold={Threshold}dB",
                                    sinkName, noiseGateThresholdDb);
                            }
                        }
                    }
                }

                // Only return settings if we found active processing
                if (dynamicRangeCompressionEnabled || noiseGateEnabled)
                {
                    return new AudioProcessingSettings(
                        dynamicRangeCompressionEnabled,
                        compressionRatio,
                        thresholdDb,
                        attackMs,
                        releaseMs,
                        noiseGateEnabled,
                        noiseGateThresholdDb);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audio processing settings for sink {SinkName}", sinkName);
                return null;
            }
        }

        /// <summary>
        /// Gets detailed information about a PulseAudio sink.
        /// </summary>
        private async Task<AudioStreamInfo> GetDetailedSinkInfoAsync(string sinkName, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _processRunner.RunAsync("pactl", "list sinks", cancellationToken);

                if (!result.Success)
                    return null;

                // Parse sink information
                return ParseSinkInfo(result.StandardOutput, sinkName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get detailed sink info for {SinkName}", sinkName);
                return null;
            }
        }

        /// <summary>
        /// Parses PulseAudio sink information from pactl output.
        /// </summary>
        private AudioStreamInfo ParseSinkInfo(string output, string sinkName)
        {
            try
            {
                var lines = output.Split('\n');
                bool inCorrectSink = false;
                double volume = 0;
                bool isMuted = false;
                string format = null;
                uint sampleRate = 0;
                bool isActive = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();

                    if (line.StartsWith("Sink #") && i + 1 < lines.Length)
                    {
                        inCorrectSink = false;
                        // Check if this is our sink by looking ahead
                        for (int j = i + 1; j < Math.Min(i + 10, lines.Length); j++)
                        {
                            if (lines[j].Trim().Contains($"Name: {sinkName}"))
                            {
                                inCorrectSink = true;
                                break;
                            }
                        }
                    }

                    if (inCorrectSink)
                    {
                        if (line.StartsWith("State:"))
                        {
                            isActive = line.Contains("RUNNING");
                        }
                        else if (line.StartsWith("Volume:"))
                        {
                            // Parse volume percentage
                            var match = VolumeRegex.Match(line);
                            if (match.Success && double.TryParse(match.Groups[1].Value, out var vol))
                            {
                                volume = vol / 100.0;
                            }
                        }
                        else if (line.StartsWith("Mute:"))
                        {
                            isMuted = line.Contains("yes");
                        }
                        else if (line.StartsWith("Sample Specification:"))
                        {
                            // Parse format and sample rate
                            var parts = line.Split(' ');
                            if (parts.Length >= 3)
                            {
                                format = parts[2];
                                if (parts.Length >= 4 && uint.TryParse(parts[3].Replace("Hz", ""), out var rate))
                                {
                                    sampleRate = rate;
                                }
                            }
                        }
                    }
                }

                if (inCorrectSink)
                {
                    return new AudioStreamInfo(sinkName, isActive, volume, isMuted, format, sampleRate);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse sink info for {SinkName}", sinkName);
                return null;
            }
        }

        /// <summary>
        /// Validates MAC address format.
        /// </summary>
        private static bool IsValidMacAddress(string address)
        {
            return !string.IsNullOrWhiteSpace(address) && MacAddressRegex.IsMatch(address);
        }

        /// <summary>
        /// Parses the pactl list modules output into structured module information.
        /// </summary>
        private List<ModuleInfo> ParseModules(string output)
        {
            var modules = new List<ModuleInfo>();
            var lines = output.Split('\n');
            ModuleInfo currentModule = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.StartsWith("Module #"))
                {
                    // Start of a new module
                    currentModule = new ModuleInfo();
                    modules.Add(currentModule);
                }
                else if (currentModule != null)
                {
                    if (line.StartsWith("Name: "))
                    {
                        currentModule.Name = line.Substring(6).Trim();
                    }
                    else if (line.StartsWith("Argument: "))
                    {
                        var argumentString = line.Substring(10).Trim();
                        currentModule.Arguments = ParseArguments(argumentString);
                    }
                }
            }

            return modules;
        }

        /// <summary>
        /// Parses module arguments string into key-value pairs.
        /// </summary>
        private Dictionary<string, string> ParseArguments(string argumentString)
        {
            var arguments = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(argumentString))
                return arguments;

            // Arguments are typically space-separated key=value pairs
            // But values might contain spaces if quoted, so we need careful parsing
            var parts = new List<string>();
            var currentPart = "";
            bool inQuotes = false;

            for (int i = 0; i < argumentString.Length; i++)
            {
                char c = argumentString[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentPart += c;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (!string.IsNullOrEmpty(currentPart))
                    {
                        parts.Add(currentPart);
                        currentPart = "";
                    }
                }
                else
                {
                    currentPart += c;
                }
            }

            if (!string.IsNullOrEmpty(currentPart))
            {
                parts.Add(currentPart);
            }

            // Parse each part as key=value
            foreach (var part in parts)
            {
                var equalIndex = part.IndexOf('=');
                if (equalIndex > 0 && equalIndex < part.Length - 1)
                {
                    var key = part.Substring(0, equalIndex).Trim();
                    var value = part.Substring(equalIndex + 1).Trim().Trim('"');
                    arguments[key] = value;
                }
            }

            return arguments;
        }

        /// <summary>
        /// Parses control values which are typically comma-separated numbers.
        /// </summary>
        private string[] ParseControlValues(string controlString)
        {
            if (string.IsNullOrWhiteSpace(controlString))
                return new string[0];

            // Control values are typically comma-separated
            return controlString.Split(',')
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrEmpty(s))
                               .ToArray();
        }

        /// <summary>
        /// Helper class to represent a PulseAudio module.
        /// </summary>
        private class ModuleInfo
        {
            public string Name { get; set; } = "";
            public Dictionary<string, string> Arguments { get; set; } = new Dictionary<string, string>();
        }
    }
}