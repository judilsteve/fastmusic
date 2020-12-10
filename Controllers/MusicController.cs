using fastmusic.DataProviders;
using fastmusic.DataTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        private readonly MusicContext musicContext;
        private readonly Configuration config;

        /// <summary>
        /// Constructor. Creates a new MusicController that will handle requests.
        /// </summary>
        /// <param name="musicProvider">Handle to the database of track metadata.</param>
        /// <param name="config">User configuration, as loaded from disk.</param>
        public MusicController(MusicContext musicProvider, Configuration config)
        {
            this.musicContext = musicProvider;
            this.config = config;
        }

        /// <param name="trackPart">A partial track name to search for.</param>
        /// <returns>A set of DbTracks, the titles of which contain <paramref name="trackPart"/>.</returns>
        [HttpGet("TracksByTitle/{trackPart}")]
        public IActionResult GetTracksByTitle(string trackPart)
        {
            var tracks = musicContext.AllTracks.AsNoTracking().Where( t =>
                t.Title!.Contains(trackPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="albumPart">A partial album name to search for.</param>
        /// <returns>A set of DbTracks, the album titles of which contain <paramref name="albumPart"/>.</returns>
        [HttpGet("TracksByAlbum/{albumPart}")]
        public IActionResult GetTracksByAlbum(string albumPart)
        {
            var tracks = musicContext.AllTracks.AsNoTracking().Where( t =>
                t.Album!.Contains(albumPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="artistPart">A partial album artist name to search for.</param>
        /// <returns>A set of DbTracks, the artist names of which contain <paramref name="artistPart"/>.</returns>
        [HttpGet("TracksByAlbumArtist/{artistPart}")]
        public IActionResult GetTracksByArtist(string artistPart)
        {
            var tracks = musicContext.AllTracks.AsNoTracking().Where( t =>
                t.AlbumArtist!.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="artistPart">A partial track performer name to search for.</param>
        /// <returns>A set of DbTracks, the performer names of which contain <paramref name="artistPart"/>.</returns>
        [HttpGet("TracksByPerformer/{artistPart}")]
        public IActionResult GetTracksByPerformer(string artistPart)
        {
            var tracks = musicContext.AllTracks.AsNoTracking().Where( t =>
                t.Performer!.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="year">A track year of release to search for.</param>
        /// <returns>A set of DbTracks with the year tag of <paramref name="year"/></returns>
        [HttpGet("TracksByYear/{year}")]
        public IActionResult GetTracksByYear(uint year)
        {
            var tracks = musicContext.AllTracks.AsNoTracking().Where( t =>
                t.Year == year
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        /// <param name="artistPart">Partial album artists name to search for.</param>
        /// <returns>A set of albums, the album artist names of which contain <paramref name="artistPart"/>, grouped by album artist.</returns>
        [HttpGet("AlbumsByArtist/{artistPart}")]
        public IActionResult GetAlbumsByArtist(string artistPart)
        {
            // Find tracks with matching album artist tag
            var result = musicContext.AllTracks
                .AsNoTracking()
                .Where(t => t.AlbumArtist != null && t.AlbumArtist.Contains(artistPart))
                .AsEnumerable()
                .GroupBy(t => new
                {
                    t.Album,
                    t.AlbumArtist,
                    t.Year
                })
                .Select(al => new
                {
                    al.Key.Album,
                    al.Key.AlbumArtist,
                    al.Key.Year,
                    Tracks = al.Count()
                })
                .GroupBy(al => al.AlbumArtist)
                .OrderBy(ar => ar.Key)
                .Select(ar => ar
                    .OrderBy(al => al.Year)
                    .ThenBy(al => al.Album));
                
            if (result == null)
            {
                return NotFound();
            }
            return new ObjectResult(result);
        }

        /// <summary>
        /// See <see cref="GetAlbumsByArtist(string)"/>
        /// </summary>
        /// <returns>All discographies in the library</returns>
        [HttpGet("AlbumsByArtist/")]
        public IActionResult GetAlbumsByArtist() => GetAlbumsByArtist("");

        /// <summary>
        /// Gets a stream of the track with the given ID.
        /// MIME type will be determined from the file extension, as specified by the user configuration.
        /// </summary>
        /// <param name="id">Unique database ID of the track.</param>
        /// <returns>A stream of the media file with ID <paramref name="id"/></returns>
        [HttpGet("MediaById/{id}")]
        public async Task<IActionResult> GetMediaById([Required] Guid? id)
        {
            var track = await musicContext.AllTracks.AsNoTracking().SingleOrDefaultAsync( t => t.Id == id );
            if(track == null)
            {
                return NotFound();
            }

            var extension = Path.GetExtension(track.FilePath).TrimStart('.');
            var stream = new FileStream(track.FilePath, FileMode.Open, FileAccess.Read);
            return new FileStreamResult(stream, config.MimeTypes[extension]);
        }
    }
}