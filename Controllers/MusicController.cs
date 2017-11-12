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
    [Route("api/[controller]")]
    public class MusicController : Controller
    {
        private readonly MusicProvider m_musicProvider;
        private readonly MediaTypeProvider m_mediaTypeProvider;

        public MusicController(MusicProvider musicProvider, MediaTypeProvider mediaTypeProvider)
        {
            m_musicProvider = musicProvider;
            m_mediaTypeProvider = mediaTypeProvider;
        }

        [HttpGet("TracksByTitle/{trackPart}")]
        public IActionResult GetTracksByTitle(string trackPart)
        {
            var tracks = m_musicProvider.AllTracks.Where( t =>
                t.Title.Contains(trackPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByAlbum/{albumPart}")]
        public IActionResult GetTracksByAlbum(string albumPart)
        {
            var tracks = m_musicProvider.AllTracks.Where( t =>
                t.Album.Contains(albumPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByAlbumArtist/{artistPart}")]
        public IActionResult GetTracksByArtist(string artistPart)
        {
            var tracks = m_musicProvider.AllTracks.Where( t =>
                t.AlbumArtist.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByPerformer/{artistPart}")]
        public IActionResult GetTracksByPerformer(string artistPart)
        {
            var tracks = m_musicProvider.AllTracks.Where( t =>
                t.Performer.Contains(artistPart)
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

        [HttpGet("TracksByYear/{year}")]
        public IActionResult GetTracksByYear(uint year)
        {
            var tracks = m_musicProvider.AllTracks.Where( t =>
                t.Year == year
            );
            if (tracks == null)
            {
                return NotFound();
            }
            return new ObjectResult(tracks);
        }

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
        public IActionResult GetAlbumsByArtist(string artistPart)
        {
            // Find tracks with matching album artist tag
            var result = m_musicProvider.AllTracks.Where( track =>
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
        public IActionResult GetAlbumsByArtist() => GetAlbumsByArtist("");

        [HttpGet("MediaById/{id}")]
        public async Task<IActionResult> GetMediaById(string id)
        {
            if(!(await m_musicProvider.AllTracks.AnyAsync(t => t.Id == id)))
            {
                return NotFound();
            }

            var fileName = m_musicProvider.AllTracks.First( t => t.Id == id ).FileName;
            var extension = Path.GetExtension(fileName).TrimStart('.');
            var stream = new FileStream( fileName, FileMode.Open, FileAccess.Read );
            string mimeType;

            foreach (var mediaType in m_mediaTypeProvider.Types)
            {
                Console.Out.WriteLine($"{mediaType.Extension} -> {mediaType.MimeType}");
            }

            if(await m_mediaTypeProvider.Types.AnyAsync( m => m.Extension == extension ))
            {
                mimeType = m_mediaTypeProvider.Types.First( m => m.Extension == extension ).MimeType;
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