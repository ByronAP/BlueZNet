using BlueZNet.Interfaces;
using Microsoft.Extensions.Logging;

namespace BlueZNet.Services
{
    /// <summary>
    /// Static factory for creating BlueZNet controllers with sensible defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This factory provides multiple ways to create BlueZNet controllers:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Zero-config: Perfect for getting started quickly</description></item>
    /// <item><description>Simple config: Just add logging</description></item>
    /// <item><description>Custom config: Use the builder for full control</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Zero configuration - uses default implementations and null logger
    /// var controller = BlueZNetController.CreateDefault();
    /// 
    /// // Simple configuration - add your logger
    /// var controllerWithLogging = BlueZNetController.CreateDefault(logger);
    /// 
    /// // Custom configuration - override specific components
    /// var customController = BlueZNetController.CreateBuilder()
    ///     .WithLogger(logger)
    ///     .WithCustomProcessRunner(new MockProcessRunner())
    ///     .WithCustomPulseAudio(new CustomPulseAudioService())
    ///     .Build();
    /// </code>
    /// </example>
    public static class BlueZNetController
    {
        /// <summary>
        /// Creates a controller with all default implementations and NullLogger.
        /// Perfect for getting started quickly with zero configuration.
        /// </summary>
        /// <returns>A fully configured BlueZNet controller with default implementations.</returns>
        /// <remarks>
        /// This is the simplest way to get started with BlueZNet. The controller will use:
        /// <list type="bullet">
        /// <item><description>NullLogger for logging (no log output)</description></item>
        /// <item><description>Default D-Bus connection factory</description></item>
        /// <item><description>Default process runner for external commands</description></item>
        /// <item><description>Default PulseAudio service</description></item>
        /// <item><description>Default device manager for BlueZ interaction</description></item>
        /// <item><description>Default media player manager</description></item>
        /// <item><description>Default audio stream monitor</description></item>
        /// </list>
        /// </remarks>
        public static IBlueZNetController CreateDefault()
        {
            return new BlueZNetControllerService();
        }

        /// <summary>
        /// Creates a controller with all default implementations and custom logger.
        /// Use this when you want logging but don't need to customize other components.
        /// </summary>
        /// <param name="logger">The logger to use for all components.</param>
        /// <returns>A fully configured BlueZNet controller with custom logging.</returns>
        /// <remarks>
        /// This method provides the same default implementations as <see cref="CreateDefault()"/>
        /// but allows you to specify a custom logger that will be used by all components.
        /// </remarks>
        public static IBlueZNetController CreateDefault(ILogger<BlueZNetControllerService> logger)
        {
            return new BlueZNetControllerService(logger);
        }

        /// <summary>
        /// Creates a builder for custom configuration of any component.
        /// Use this when you need to override specific implementations.
        /// </summary>
        /// <returns>A builder that allows fluent configuration of all components.</returns>
        /// <remarks>
        /// <para>
        /// The builder allows you to customize any aspect of the BlueZNet controller:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Replace the logger</description></item>
        /// <item><description>Use custom D-Bus connection factory</description></item>
        /// <item><description>Implement custom process execution</description></item>
        /// <item><description>Override PulseAudio integration</description></item>
        /// <item><description>Customize device management</description></item>
        /// <item><description>Replace media player management</description></item>
        /// <item><description>Implement custom audio monitoring</description></item>
        /// </list>
        /// <para>
        /// Any component not explicitly configured will use sensible defaults.
        /// </para>
        /// </remarks>
        public static BlueZNetControllerBuilder CreateBuilder()
        {
            return new BlueZNetControllerBuilder();
        }
    }
}