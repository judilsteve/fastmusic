using fastmusic.DataProviders;
using fastmusic.DataTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fastmusic.Controllers
{
    /// <summary>
    /// Provides music metadata and streams.
    /// </summary>
    [Route("api/[controller]")]
    public class MusicController : Controller
    {
        /// <summary>
        /// Handle to the database of track metadata
        /// </summary>
        private readonly MusicProvider musicProvider;

        /// <summary>
        /// User configuration, as loaded from disk.
        /// </summary>
        private readonly Config config;

        /// <summary>
        /// Constructor. Creates a new MusicController that will handle requests.
        /// </summary>
        /// <param name="musicProvider">Handle to the database of track metadata.</param>
        /// <param name="config">User configuration, as loaded from disk.</param>
        public MusicController(MusicProvider musicProvider, Config config)
        {
            this.musicProvider = musicProvider;
            this.config = config;
        }

        /// <param name="trackPart">A partial track name to search for.</param>
        /// <returns>A set of DbTracks, the titles of which contain @param trackPart.</returns>
        [HttpGet("TracksByTitle/{trackPart}")]
        public IActionResult GetTracksByTitle(string trackPart)
        {
            var tracks = musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Title.Contains(trackPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="albumPart">A partial album name to search for.</param>
        /// <returns>A set of DbTracks, the album titles of which contain @param albumPart.</returns>
        [HttpGet("TracksByAlbum/{albumPart}")]
        public IActionResult GetTracksByAlbum(string albumPart)
        {
            var tracks = musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Album.Contains(albumPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="artistPart">A partial album artist name to search for.</param>
        /// <returns>A set of DbTracks, the artist names of which contain @param artistPart.</returns>
        [HttpGet("TracksByAlbumArtist/{artistPart}")]
        public IActionResult GetTracksByArtist(string artistPart)
        {
            var tracks = musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.AlbumArtist.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="artistPart">A partial track performer name to search for.</param>
        /// <returns>A set of DbTracks, the performer names of which contain @param artistPart.</returns>
        [HttpGet("TracksByPerformer/{artistPart}")]
        public IActionResult GetTracksByPerformer(string artistPart)
        {
            var tracks = musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Performer.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="year">A track year of release to search for.</param>
        /// <returns>A set of DbTracks with the year tag of @param year</returns>
        [HttpGet("TracksByYear/{year}")]
        public IActionResult GetTracksByYear(uint year)
        {
            var tracks = musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Year == year
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

         /// <summary>
         /// Data class for album metadata.
         /// Does not store metadata for the individual tracks in the album
         /// </summary>
        private class Album
        {
            /// <summary>
            /// Title of this album.
            /// </summary>
            public string Title { get; set; }
            
            /// <summary>
            /// Album artist for this album.
            /// </summary>
            public string Artist { get; set; }

            /// <summary>
            /// Release year for this album.
            /// </summary>
            public uint Year { get; set; }

            /// <summary>
            /// Number of tracks in this album.
            /// </summary>
            public uint Tracks { get; set; }

             /// <summary>
             /// Constructor.
             /// Creates an album from the set of tracks comprising the album.
             /// Metadata is taken from the first track in @tracks.
             /// </summary>
             /// <param name="tracks">A collection of tracks comprising an album.</param>
            public Album(IEnumerable<DbTrack> tracks)
            {
                DbTrack first = tracks.First();
                Title = first.Album;
                Artist = first.AlbumArtist;
                Year = first.Year;
                Tracks = (uint) tracks.Count();
            }
        }

        /// <param name="artistPart">Partial album artists name to search for.</param>
        /// <returns>A set of albums, the album artist names of which contain @param artistPart, grouped by album artist.</returns>
        [HttpGet("AlbumsByArtist/{artistPart}")]
        public IActionResult GetAlbumsByArtist(string artistPart)
        {
            // Find tracks with matching album artist tag
            var result = musicProvider.AllTracks.AsNoTracking().Where( track =>
                track.AlbumArtist.Contains(artistPart)
            )
            // Group into albums
            .GroupBy( track =>
                track.Album
            )
            .Select( trackList =>
                new Album(trackList)
            )
            // Group into discographies
            .GroupBy( album =>
                album.Artist
            )
            // Order discographies by artist
            .OrderBy( discography =>
                discography.First().Artist
            )
            // Sort each discography by date
            .Select( discography =>
                discography.OrderBy( album =>
                    album.Year
                )
            );
            if (result == null)
            {
                return NotFound();
            }
            return new ObjectResult(result);
        }

        /// <summary>
        /// @see GetAlbumsByArtist(string artistPart)
        /// </summary>
        /// <returns>All discographies in the library</returns>
        [HttpGet("AlbumsByArtist/")]
        public IActionResult GetAlbumsByArtist() => GetAlbumsByArtist("");

        /// <summary>
        /// Gets a stream of the track with the given ID.
        /// MIME type will be determined from the file extension, as specified by the user configuration.
        /// </summary>
        /// <param name="id">Unique database ID of the track.</param>
        /// <returns>A stream of the media file with ID @param id</returns>
        [HttpGet("MediaById/{id}")]
        public async Task<IActionResult> GetMediaById(string id)
        {
            var track = await musicProvider.AllTracks.AsNoTracking().SingleOrDefaultAsync( t => t.Id == id );
            if(track == null)
            {
                return NotFound();
            }

            var extension = Path.GetExtension(track.FileName).TrimStart('.');
            var stream = new FileStream( track.FileName, FileMode.Open, FileAccess.Read );
            return new FileStreamResult(stream, config.MimeTypes[extension]);
        }
    }
}