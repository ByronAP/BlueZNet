using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Interfaces
{
    /// <summary>
    /// Factory for creating and managing D-Bus connections to BlueZ.
    /// Implement this interface to customize D-Bus connection behavior.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts D-Bus connection management, allowing you to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Customize connection parameters and timeout behavior</description></item>
    /// <item><description>Implement connection pooling or caching strategies</description></item>
    /// <item><description>Add connection retry logic or fallback mechanisms</description></item>
    /// <item><description>Mock D-Bus connections for testing scenarios</description></item>
    /// </list>
    /// <para>
    /// The default implementation handles standard system bus connections to BlueZ.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Custom implementation with retry logic
    /// public class RetryingDBusConnectionFactory : IDBusConnectionFactory
    /// {
    ///     public async Task&lt;Connection&gt; CreateSystemConnectionAsync(CancellationToken cancellationToken = default)
    ///     {
    ///         for (int attempt = 0; attempt &lt; 3; attempt++)
    ///         {
    ///             try
    ///             {
    ///                 return Connection.System;
    ///             }
    ///             catch when (attempt &lt; 2)
    ///             {
    ///                 await Task.Delay(1000, cancellationToken);
    ///             }
    ///         }
    ///         throw new BlueZNetException("Failed to connect after 3 attempts");
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IDBusConnectionFactory
    {
        /// <summary>
        /// Creates a connection to the system D-Bus for BlueZ communication.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the connection attempt.</param>
        /// <returns>A D-Bus connection that can be used to communicate with BlueZ.</returns>
        /// <exception cref="Exceptions.BlueZNetException">Thrown when the D-Bus connection cannot be established.</exception>
        Task<Connection> CreateSystemConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a proxy object for interacting with a specific D-Bus service.
        /// </summary>
        /// <typeparam name="T">The D-Bus interface type to create a proxy for.</typeparam>
        /// <param name="connection">The D-Bus connection to use.</param>
        /// <param name="serviceName">The name of the D-Bus service (e.g., "org.bluez").</param>
        /// <param name="objectPath">The object path to connect to.</param>
        /// <returns>A proxy object that implements the specified D-Bus interface.</returns>
        T CreateProxy<T>(Connection connection, string serviceName, string objectPath) where T : IDBusObject;

        /// <summary>
        /// Properly disposes of a D-Bus connection and releases associated resources.
        /// </summary>
        /// <param name="connection">The connection to dispose.</param>
        /// <returns>A task representing the asynchronous disposal operation.</returns>
        Task DisposeConnectionAsync(Connection connection);
    }
}
