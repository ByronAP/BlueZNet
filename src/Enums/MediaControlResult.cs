namespace BlueZNet.Enums
{
    /// <summary>
    /// Represents the result of a media control operation.
    /// </summary>
    public enum MediaControlResult
    {
        /// <summary>
        /// The operation completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// The specified device was not found.
        /// </summary>
        DeviceNotFound,

        /// <summary>
        /// The device is not currently connected.
        /// </summary>
        DeviceNotConnected,

        /// <summary>
        /// The device does not have an available media player.
        /// </summary>
        MediaPlayerNotAvailable,

        /// <summary>
        /// The command failed to execute.
        /// </summary>
        CommandFailed,

        /// <summary>
        /// The requested feature is not supported by the device.
        /// </summary>
        NotSupported
    }
}