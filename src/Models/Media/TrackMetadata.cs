namespace BlueZNet.Models.Media
{
    /// <summary>
    /// Contains metadata information about a music track.
    /// </summary>
    public class TrackMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackMetadata"/> class.
        /// </summary>
        /// <param name="title">The title of the track.</param>
        /// <param name="artist">The artist name.</param>
        /// <param name="album">The album name.</param>
        /// <param name="genre">The genre of the track.</param>
        /// <param name="numberOfTracks">The total number of tracks in the album.</param>
        /// <param name="trackNumber">The track number within the album.</param>
        /// <param name="duration">The duration of the track in milliseconds.</param>
        public TrackMetadata(
            string title = null,
            string artist = null,
            string album = null,
            string genre = null,
            uint? numberOfTracks = null,
            uint? trackNumber = null,
            uint? duration = null)
        {
            Title = title;
            Artist = artist;
            Album = album;
            Genre = genre;
            NumberOfTracks = numberOfTracks;
            TrackNumber = trackNumber;
            Duration = duration;
        }

        /// <summary>
        /// Gets the title of the track.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the artist name.
        /// </summary>
        public string Artist { get; }

        /// <summary>
        /// Gets the album name.
        /// </summary>
        public string Album { get; }

        /// <summary>
        /// Gets the genre of the track.
        /// </summary>
        public string Genre { get; }

        /// <summary>
        /// Gets the total number of tracks in the album.
        /// </summary>
        public uint? NumberOfTracks { get; }

        /// <summary>
        /// Gets the track number within the album.
        /// </summary>
        public uint? TrackNumber { get; }

        /// <summary>
        /// Gets the duration of the track in milliseconds.
        /// </summary>
        public uint? Duration { get; }
    }
}