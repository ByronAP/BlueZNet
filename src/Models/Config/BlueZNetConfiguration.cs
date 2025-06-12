namespace BlueZNet.Models.Config
{
    /// <summary>
    /// Configuration settings for BlueZNet operations.
    /// </summary>
    public class BlueZNetConfiguration
    {
        /// <summary>
        /// Gets or sets the timeout for external process execution in milliseconds.
        /// </summary>
        public int ProcessTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the interval for audio stream monitoring in seconds.
        /// </summary>
        public int AudioStreamMonitorIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the delay after device disconnection before reconnecting in milliseconds.
        /// </summary>
        public int ProfileSwitchDisconnectDelayMs { get; set; } = 3000;

        /// <summary>
        /// Gets or sets the delay after device connection to allow profile negotiation in milliseconds.
        /// </summary>
        public int ProfileSwitchConnectDelayMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets the timeout for D-Bus operations in milliseconds.
        /// </summary>
        public int DBusOperationTimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed operations.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets whether to enable verbose logging for debugging.
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;

        /// <summary>
        /// Creates a default configuration instance.
        /// </summary>
        public static BlueZNetConfiguration Default => new BlueZNetConfiguration();
    }
}
