using BlueZNet.Interfaces;
using BlueZNet.Models.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueZNet.Services
{
    /// <summary>
    /// Builder for creating customized BlueZNet controllers.
    /// All dependencies are optional - sensible defaults will be provided for anything not specified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This builder implements the fluent builder pattern, allowing you to chain configuration calls:
    /// </para>
    /// <code>
    /// var controller = new BlueZNetControllerBuilder()
    ///     .WithLogger(myLogger)
    ///     .WithCustomDBusFactory(myDBusFactory)
    ///     .WithCustomProcessRunner(myProcessRunner)
    ///     .Build();
    /// </code>
    /// <para>
    /// Any component not explicitly configured will use the default implementation.
    /// This ensures you only need to customize the parts you care about.
    /// </para>
    /// </remarks>
    public class BlueZNetControllerBuilder
    {
        private ILogger<BlueZNetControllerService> _logger = NullLogger<BlueZNetControllerService>.Instance;
        private IDBusConnectionFactory _dbusFactory = null;
        private IProcessRunner _processRunner = null;
        private IPulseAudioService _pulseAudioService = null;
        private IBlueZDeviceManager _deviceManager = null;
        private IMediaPlayerManager _mediaPlayerManager = null;
        private IAudioStreamMonitor _audioStreamMonitor = null;
        private BlueZNetConfiguration _configuration = null;

        /// <summary>
        /// Sets the logger to use for all components.
        /// </summary>
        /// <param name="logger">The logger instance, or null to use NullLogger.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public BlueZNetControllerBuilder WithLogger(ILogger<BlueZNetControllerService> logger)
        {
            _logger = logger ?? NullLogger<BlueZNetControllerService>.Instance;
            return this;
        }

        /// <summary>
        /// Sets the configuration for timeouts and behavior.
        /// </summary>
        public BlueZNetControllerBuilder WithConfiguration(BlueZNetConfiguration configuration)
        {
            _configuration = configuration;
            return this;
        }

        /// <summary>
        /// Sets a custom D-Bus connection factory.
        /// </summary>
        /// <param name="dbusFactory">The custom D-Bus connection factory implementation.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <remarks>
        /// Use this to customize D-Bus connection behavior, add connection pooling,
        /// implement retry logic, or mock D-Bus connections for testing.
        /// </remarks>
        public BlueZNetControllerBuilder WithCustomDBusFactory(IDBusConnectionFactory dbusFactory)
        {
            _dbusFactory = dbusFactory;
            return this;
        }

        /// <summary>
        /// Sets a custom process runner for external command execution.
        /// </summary>
        /// <param name="processRunner">The custom process runner implementation.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <remarks>
        /// Use this to customize how external commands (pactl, bluetoothctl) are executed,
        /// add retry logic, implement timeouts, or mock process execution for testing.
        /// </remarks>
        public BlueZNetControllerBuilder WithCustomProcessRunner(IProcessRunner processRunner)
        {
            _processRunner = processRunner;
            return this;
        }

        /// <summary>
        /// Sets a custom PulseAudio service implementation.
        /// </summary>
        /// <param name="pulseAudioService">The custom PulseAudio service implementation.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <remarks>
        /// Use this to customize audio stream management, implement alternative audio systems,
        /// add audio processing features, or mock audio operations for testing.
        /// </remarks>
        public BlueZNetControllerBuilder WithCustomPulseAudio(IPulseAudioService pulseAudioService)
        {
            _pulseAudioService = pulseAudioService;
            return this;
        }

        /// <summary>
        /// Sets a custom device manager for BlueZ device management.
        /// </summary>
        /// <param name="deviceManager">The custom device manager implementation.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <remarks>
        /// Use this to customize device discovery, implement device filtering,
        /// add custom capability detection, or mock device management for testing.
        /// </remarks>
        public BlueZNetControllerBuilder WithCustomDeviceManager(IBlueZDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
            return this;
        }

        /// <summary>
        /// Sets a custom media player manager for media control operations.
        /// </summary>
        /// <param name="mediaPlayerManager">The custom media player manager implementation.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <remarks>
        /// Use this to customize media control behavior, implement alternative media protocols,
        /// add media operation validation, or mock media operations for testing.
        /// </remarks>
        public BlueZNetControllerBuilder WithCustomMediaManager(IMediaPlayerManager mediaPlayerManager)
        {
            _mediaPlayerManager = mediaPlayerManager;
            return this;
        }

        /// <summary>
        /// Sets a custom audio stream monitor for audio monitoring operations.
        /// </summary>
        /// <param name="audioStreamMonitor">The custom audio stream monitor implementation.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <remarks>
        /// Use this to customize audio monitoring behavior, implement alternative monitoring strategies,
        /// add audio processing features, or mock audio monitoring for testing.
        /// </remarks>
        public BlueZNetControllerBuilder WithCustomAudioMonitor(IAudioStreamMonitor audioStreamMonitor)
        {
            _audioStreamMonitor = audioStreamMonitor;
            return this;
        }

        /// <summary>
        /// Builds the controller with provided customizations and defaults for everything else.
        /// </summary>
        /// <returns>A fully configured BlueZNet controller with your customizations and sensible defaults.</returns>
        /// <remarks>
        /// <para>
        /// This method creates the final controller instance using:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Your custom implementations where provided</description></item>
        /// <item><description>Default implementations for everything else</description></item>
        /// <item><description>Proper dependency injection between all components</description></item>
        /// </list>
        /// <para>
        /// The resulting controller will have the exact same public API as controllers
        /// created through other factory methods, ensuring full compatibility.
        /// </para>
        /// </remarks>
        public IBlueZNetController Build()
        {
            // Use provided configuration or create default
            var config = _configuration ?? BlueZNetConfiguration.Default;

            // Create dependencies in correct order
            var dbusFactory = _dbusFactory ?? new DefaultDBusConnectionFactory(_logger);
            var processRunner = _processRunner ?? new DefaultProcessRunner(_logger, config);
            var pulseAudioService = _pulseAudioService ?? new DefaultPulseAudioService(processRunner, _logger, config);
            var deviceManager = _deviceManager ?? new DefaultBlueZDeviceManager(dbusFactory, processRunner, _logger, config);
            var mediaPlayerManager = _mediaPlayerManager ?? new DefaultMediaPlayerManager(dbusFactory, deviceManager, _logger, config);
            var audioStreamMonitor = _audioStreamMonitor ?? new DefaultAudioStreamMonitor(pulseAudioService, deviceManager, dbusFactory, _logger, config);

            return new BlueZNetControllerService(
                _logger,
                dbusFactory,
                processRunner,
                pulseAudioService,
                deviceManager,
                mediaPlayerManager,
                audioStreamMonitor,
                config
            );
        }
    }
}