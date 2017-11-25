using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using fastmusic.DataProviders;
using fastmusic.DataTypes;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System;

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
        private readonly MusicProvider m_musicProvider;

        /// <summary>
        /// User configuration, as loaded from disk.
        /// </summary>
        private readonly Config m_config;

        /// <summary>
        /// Constructor. Creates a new MusicController that will handle requests.
        /// </summary>
        /// <param name="musicProvider">Handle to the database of track metadata.</param>
        /// <param name="config">User configuration, as loaded from disk.</param>
        public MusicController(MusicProvider musicProvider, Config config)
        {
            m_musicProvider = musicProvider;
            m_config = config;
        }

        [HttpGet("TracksByTitle/{trackPart}")]
        /**
         * @return A set of DbTracks, the titles of which contain @param trackPart
         */
        public IActionResult GetTracksByTitle(string trackPart)
        {
            var tracks = m_musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Title.Contains(trackPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByAlbum/{albumPart}")]
        /**
         * @return A set of DbTracks, the album titles of which contain @param albumPart
         */
        public IActionResult GetTracksByAlbum(string albumPart)
        {
            var tracks = m_musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Album.Contains(albumPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByAlbumArtist/{artistPart}")]
        /**
         * @return A set of DbTracks, the artist names of which contain @param artistPart
         */
        public IActionResult GetTracksByArtist(string artistPart)
        {
            var tracks = m_musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.AlbumArtist.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByPerformer/{artistPart}")]
        /**
         * @return A set of DbTracks, the performer names of which contain @param artistPart
         */
        public IActionResult GetTracksByPerformer(string artistPart)
        {
            var tracks = m_musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Performer.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByYear/{year}")]
        /**
         * @return A set of DbTracks with the year tag of @param year
         */
        public IActionResult GetTracksByYear(uint year)
        {
            var tracks = m_musicProvider.AllTracks.AsNoTracking().Where( t =>
                t.Year == year
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /**
         * Stores metadata about an album, but not the tracks within the album
         */
        private class Album
        {
            public string Title { get; set; }
            public string Artist { get; set; }
            public uint Year { get; set; }

            public uint Tracks { get; set; }

            /**
             * Creates an album from the set of tracks comprising the album
             */
            public Album(IEnumerable<DbTrack> tracks)
            {
                DbTrack first = tracks.First();
                Title = first.Album;
                Artist = first.AlbumArtist;
                Year = first.Year;
                Tracks = (uint) tracks.Count();
            }
        }

        [HttpGet("AlbumsByArtist/{artistPart}")]
        /**
         * @return A set of albums, the album artist names of which contain artistPart
         * Grouped by album artist
         */
        public IActionResult GetAlbumsByArtist(string artistPart)
        {
            // Find tracks with matching album artist tag
            var result = m_musicProvider.AllTracks.AsNoTracking().Where( track =>
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

        [HttpGet("AlbumsByArtist/")]
        /**
         * @return All discographies in the library
         */
        public IActionResult GetAlbumsByArtist() => GetAlbumsByArtist("");

        [HttpGet("MediaById/{id}")]
        /**
         * @return A stream of the media file with ID @id
         * MIME type will be determined from the file extension, as specified by the config file
         * If the extension does not have a MIME type mapping in the config file, a default guess will be used
         */
        public async Task<IActionResult> GetMediaById(string id)
        {
            var track = await m_musicProvider.AllTracks.AsNoTracking().SingleOrDefaultAsync( t => t.Id == id );
            if(track == null)
            {
                return NotFound();
            }
            var extension = Path.GetExtension(track.FileName).TrimStart('.');
            var stream = new FileStream( track.FileName, FileMode.Open, FileAccess.Read );
            string mimeType;

            foreach (var mediaType in m_config.MimeTypes)
            {
                Console.Out.WriteLine($"{mediaType.Key} -> {mediaType.Value}");
            }

            if(m_config.MimeTypes[extension] != null)
            {
                mimeType = m_config.MimeTypes[extension];
            }
            else
            {
                Console.Out.WriteLine($"Guessing MIME type for extension {extension}");
                // Have a guess
                mimeType = "audio/mp4";
            }

            return new FileStreamResult(stream, mimeType);
        }
    }
}