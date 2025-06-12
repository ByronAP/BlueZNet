using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Interfaces.DBus
{
    /// <summary>
    /// D-Bus properties interface.
    /// </summary>
    [DBusInterface("org.freedesktop.DBus.Properties")]
    public interface IProperties : IDBusObject
    {
        /// <summary>
        /// Gets a single property value.
        /// </summary>
        Task<object> GetAsync(string interfaceName, string propertyName);

        /// <summary>
        /// Gets all properties for an interface.
        /// </summary>
        Task<IDictionary<string, object>> GetAllAsync(string interfaceName);

        /// <summary>
        /// Sets a property value.
        /// </summary>
        Task SetAsync(string interfaceName, string propertyName, object value);

        /// <summary>
        /// Watches for property changes.
        /// </summary>
        Task<IDisposable> WatchPropertiesChangedAsync(Action<(string interfaceName, IDictionary<string, object> changedProperties, string[] invalidatedProperties)> handler, Action<Exception> onError = null);
    }
}