using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Interfaces.DBus
{
    /// <summary>
    /// BlueZ D-Bus interface for device control.
    /// </summary>
    [DBusInterface("org.bluez.Device1")]
    public interface IDevice : IDBusObject
    {
        /// <summary>
        /// Disconnects the device.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Connects to the device.
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Pairs with the device.
        /// </summary>
        Task PairAsync();

        /// <summary>
        /// Cancels pairing.
        /// </summary>
        Task CancelPairingAsync();

        /// <summary>
        /// Gets a property value.
        /// </summary>
        Task<T> GetAsync<T>(string prop);

        /// <summary>
        /// Gets all properties.
        /// </summary>
        Task<IDictionary<string, object>> GetAllAsync();

        /// <summary>
        /// Sets a property value.
        /// </summary>
        Task SetAsync(string prop, object val);

        /// <summary>
        /// Watches for property changes.
        /// </summary>
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler, Action<Exception> onError = null);
    }
}