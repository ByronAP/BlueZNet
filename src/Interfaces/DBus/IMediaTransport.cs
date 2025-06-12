using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Interfaces.DBus
{
    /// <summary>
    /// BlueZ D-Bus interface for media transport control.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MediaTransport objects represent the A2DP transport channels between
    /// BlueZ and connected audio devices. They handle:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Audio codec configuration and negotiation</description></item>
    /// <item><description>Transport state management (idle, pending, active)</description></item>
    /// <item><description>Volume control for A2DP absolute volume</description></item>
    /// <item><description>Acquiring and releasing transport resources</description></item>
    /// </list>
    /// <para>
    /// This interface allows applications to control audio transport properties
    /// and manage codec switching operations.
    /// </para>
    /// </remarks>
    [DBusInterface("org.bluez.MediaTransport1")]
    public interface IMediaTransport : IDBusObject
    {
        /// <summary>
        /// Acquires transport for streaming with optional configuration.
        /// </summary>
        /// <returns>A tuple containing file descriptor and properties for the acquired transport.</returns>
        /// <exception cref="InvalidOperationException">Thrown when transport cannot be acquired.</exception>
        /// <remarks>
        /// <para>
        /// Acquiring a transport reserves it for audio streaming. The returned file descriptor
        /// can be used for direct audio I/O operations. This is typically called by audio
        /// applications before starting playback.
        /// </para>
        /// <para>
        /// The properties dictionary may contain transport-specific configuration such as:
        /// </para>
        /// <list type="bullet">
        /// <item><description>MTU (Maximum Transmission Unit)</description></item>
        /// <item><description>Delay compensation values</description></item>
        /// <item><description>Codec-specific parameters</description></item>
        /// </list>
        /// </remarks>
        Task<(int fd, IDictionary<string, object> properties)> AcquireAsync();

        /// <summary>
        /// Tries to acquire transport with optional access type.
        /// </summary>
        /// <param name="accessType">The type of access requested (e.g., "rw", "r", "w").</param>
        /// <returns>A tuple containing file descriptor and properties for the acquired transport.</returns>
        /// <remarks>
        /// This method allows specifying the access type for the transport acquisition.
        /// Different access types may be supported depending on the transport capabilities.
        /// </remarks>
        Task<(int fd, IDictionary<string, object> properties)> TryAcquireAsync(string accessType);

        /// <summary>
        /// Releases the acquired transport.
        /// </summary>
        /// <returns>A task representing the asynchronous release operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when transport is not currently acquired.</exception>
        /// <remarks>
        /// <para>
        /// Releasing a transport frees the resources and makes it available for other
        /// applications or for reconfiguration. This should be called when audio
        /// streaming is stopped or when switching codecs.
        /// </para>
        /// <para>
        /// After release, the file descriptor becomes invalid and should not be used.
        /// </para>
        /// </remarks>
        Task ReleaseAsync();

        /// <summary>
        /// Gets a property value from the transport.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="propertyName">The name of the property to retrieve.</param>
        /// <returns>The property value cast to the specified type.</returns>
        /// <exception cref="ArgumentException">Thrown when the property name is invalid.</exception>
        /// <exception cref="InvalidCastException">Thrown when the property cannot be cast to the specified type.</exception>
        /// <remarks>
        /// Common properties include:
        /// <list type="bullet">
        /// <item><description>"Device" - Object path of the associated device</description></item>
        /// <item><description>"UUID" - A2DP service UUID</description></item>
        /// <item><description>"Codec" - Codec identifier byte</description></item>
        /// <item><description>"Configuration" - Codec configuration bytes</description></item>
        /// <item><description>"State" - Current transport state</description></item>
        /// <item><description>"Volume" - Absolute volume level (0-127)</description></item>
        /// </list>
        /// </remarks>
        Task<T> GetAsync<T>(string propertyName);

        /// <summary>
        /// Gets all properties from the transport.
        /// </summary>
        /// <returns>A dictionary containing all transport properties.</returns>
        /// <remarks>
        /// This method is more efficient than calling GetAsync multiple times
        /// when you need access to several properties.
        /// </remarks>
        Task<IDictionary<string, object>> GetAllAsync();

        /// <summary>
        /// Sets a property value on the transport.
        /// </summary>
        /// <param name="propertyName">The name of the property to set.</param>
        /// <param name="value">The new value for the property.</param>
        /// <returns>A task representing the asynchronous set operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the property name is invalid or read-only.</exception>
        /// <exception cref="ArgumentException">Thrown when the value type is incompatible.</exception>
        /// <remarks>
        /// <para>
        /// Not all properties are writable. Common writable properties include:
        /// </para>
        /// <list type="bullet">
        /// <item><description>"Volume" - Set absolute volume (0-127)</description></item>
        /// </list>
        /// <para>
        /// Properties like "Device", "UUID", "Codec", and "State" are typically read-only.
        /// </para>
        /// </remarks>
        Task SetAsync(string propertyName, object value);

        /// <summary>
        /// Watches for property changes on the transport.
        /// </summary>
        /// <param name="handler">Action to call when properties change.</param>
        /// <param name="onError">Optional error handler for subscription errors.</param>
        /// <returns>A disposable subscription that can be used to stop watching.</returns>
        /// <remarks>
        /// <para>
        /// Property change notifications are useful for monitoring:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Transport state changes (idle → pending → active)</description></item>
        /// <item><description>Volume changes from the remote device</description></item>
        /// <item><description>Codec reconfiguration events</description></item>
        /// </list>
        /// <para>
        /// Remember to dispose the returned subscription to avoid memory leaks.
        /// </para>
        /// </remarks>
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception> onError = null);
    }
}
