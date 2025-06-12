using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace BlueZNet.Interfaces.DBus
{
    /// <summary>
    /// BlueZ D-Bus interface for media player control.
    /// </summary>
    [DBusInterface("org.bluez.MediaPlayer1")]
    public interface IMediaPlayer : IDBusObject
    {
        /// <summary>
        /// Starts or resumes playback.
        /// </summary>
        Task PlayAsync();

        /// <summary>
        /// Pauses playback.
        /// </summary>
        Task PauseAsync();

        /// <summary>
        /// Stops playback.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Skips to the next track.
        /// </summary>
        Task NextAsync();

        /// <summary>
        /// Goes to the previous track.
        /// </summary>
        Task PreviousAsync();

        /// <summary>
        /// Fast forwards the current track.
        /// </summary>
        Task FastForwardAsync();

        /// <summary>
        /// Rewinds the current track.
        /// </summary>
        Task RewindAsync();

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