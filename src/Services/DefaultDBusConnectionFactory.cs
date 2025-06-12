using BlueZNet.Exceptions;
using BlueZNet.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Services
{
    /// <summary>
    /// Default implementation of D-Bus connection factory for BlueZ integration.
    /// </summary>
    public class DefaultDBusConnectionFactory : IDBusConnectionFactory
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDBusConnectionFactory"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public DefaultDBusConnectionFactory(ILogger logger = null)
        {
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        }

        /// <inheritdoc />
        public async Task<Connection> CreateSystemConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Creating D-Bus system connection");

                var connection = new Connection(Address.System);
                await connection.ConnectAsync();

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("D-Bus system connection created successfully");
                return connection;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("D-Bus connection creation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create D-Bus system connection");
                throw new BlueZNetException("Failed to create D-Bus system connection", ex);
            }
        }

        /// <inheritdoc />
        public T CreateProxy<T>(Connection connection, string serviceName, string objectPath) where T : IDBusObject
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));
            if (string.IsNullOrWhiteSpace(objectPath))
                throw new ArgumentException("Object path cannot be null or empty", nameof(objectPath));

            try
            {
                _logger.LogTrace("Creating D-Bus proxy for {Interface} at {ServiceName}{ObjectPath}",
                    typeof(T).Name, serviceName, objectPath);

                var proxy = connection.CreateProxy<T>(serviceName, objectPath);

                _logger.LogTrace("D-Bus proxy created successfully for {Interface}", typeof(T).Name);
                return proxy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create D-Bus proxy for {Interface} at {ServiceName}{ObjectPath}",
                    typeof(T).Name, serviceName, objectPath);
                throw new BlueZNetException($"Failed to create D-Bus proxy for {typeof(T).Name}", ex);
            }
        }

        /// <inheritdoc />
        public async Task DisposeConnectionAsync(Connection connection)
        {
            if (connection == null)
                return;

            try
            {
                _logger.LogDebug("Disposing D-Bus connection");

                await Task.Run(() => connection.Dispose());

                _logger.LogDebug("D-Bus connection disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing D-Bus connection");
            }
        }
    }
}