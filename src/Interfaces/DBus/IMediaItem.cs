using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Interfaces.DBus
{
    /// <summary>
    /// BlueZ D-Bus interface for media items.
    /// </summary>
    [DBusInterface("org.bluez.MediaItem1")]
    public interface IMediaItem : IDBusObject
    {
        /// <summary>
        /// Plays this media item.
        /// </summary>
        Task PlayAsync();

        /// <summary>
        /// Adds this item to the now playing queue.
        /// </summary>
        Task AddtoNowPlayingAsync();

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