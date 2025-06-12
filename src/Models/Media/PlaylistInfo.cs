namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Contains information about the current playlist.
    /// </summary>
    public class PlaylistInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the playlist.</param>
        /// <param name="totalTracks">The total number of tracks in the playlist.</param>
        /// <param name="currentTrackIndex">The index of the currently playing track (0-based).</param>
        public PlaylistInfo(string name = null, uint? totalTracks = null, uint? currentTrackIndex = null)
        {
            Name = name;
            TotalTracks = totalTracks;
            CurrentTrackIndex = currentTrackIndex;
        }

        /// <summary>
        /// Gets the name of the playlist.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the total number of tracks in the playlist.
        /// </summary>
        public uint? TotalTracks { get; }

        /// <summary>
        /// Gets the index of the currently playing track (0-based).
        /// </summary>
        public uint? CurrentTrackIndex { get; }
    }
}