using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Interfaces.DBus
{
    /// <summary>
    /// BlueZ D-Bus interface for media folder browsing.
    /// </summary>
    [DBusInterface("org.bluez.MediaFolder1")]
    public interface IMediaFolder : IDBusObject
    {
        /// <summary>
        /// Lists items in the folder.
        /// </summary>
        Task<(ObjectPath[] items, IDictionary<string, object>[] properties)> ListItemsAsync();

        /// <summary>
        /// Searches for items in the folder.
        /// </summary>
        Task<ObjectPath> SearchAsync(string query, IDictionary<string, object> filter);

        /// <summary>
        /// Gets the number of items in the folder.
        /// </summary>
        Task<uint> GetNumberOfItemsAsync();

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
    }
}