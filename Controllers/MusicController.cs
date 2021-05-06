using fastmusic.DataProviders;
using fastmusic.Utils;
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

        public class TrackDto
        {
            public Guid Id { get; set; }
            public string? Title { get; set; }
            public string? Performer { get; set; }
        }

        public class AlbumDto
        {
            public string? Artist { get; set; }
            public string? Title { get; set; }
            public uint? Year { get; set; }
            public IList<TrackDto> Tracks { get; set; } = null!;
        }

        private struct AlbumKey : IEquatable<AlbumKey>
        {
            public readonly string? Artist;
            public readonly string? Title;
            public readonly uint? Year;

            private readonly int hashCode;

            public override int GetHashCode() => hashCode;

            public bool Equals(AlbumKey other)
            {
                if(hashCode != other.GetHashCode()) return false;
                if(Year != other.Year) return false;
                if(Title != other.Title) return false;
                if(Artist != other.Artist) return false;
                return true;
            }

            public AlbumKey(string? artist, string? title, uint? year)
            {
                Artist = artist;
                Title = title;
                Year = year;
                hashCode = HashCode.Combine(Artist, Title, Year);
            }
        }

        public async IAsyncEnumerable<AlbumDto> GetEntireCollection() // TODO Test other options for this (performance-wise) e.g. client-side GroupBy
        {
            var query = musicContext.AllTracks
                .OrderBy(t => t.AlbumArtist)
                .ThenBy(t => t.Year)
                .ThenBy(t => t.Album)
                .ThenBy(t => t.TrackNumber)
                .Select(t => new
                {
                    AlbumKey = new
                    {
                        t.AlbumArtist,
                        t.Year,
                        t.Album
                    },
                    TrackDto = new TrackDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Performer = t.Performer
                    }
                })
                .AsAsyncEnumerable();

            AlbumDto? currentAlbum = null;
            AlbumKey? currentAlbumKey = null;
            await foreach(var track in query)
            {
                var nextTrackAlbumKey = new AlbumKey(track.AlbumKey.AlbumArtist, track.AlbumKey.Album, track.AlbumKey.Year);
                if(currentAlbum is null || !currentAlbumKey!.Value.Equals(nextTrackAlbumKey))
                {
                    if(currentAlbum is not null) yield return currentAlbum;
                    currentAlbum = new AlbumDto
                    {
                        Artist = nextTrackAlbumKey.Artist,
                        Title = nextTrackAlbumKey.Title,
                        Year = nextTrackAlbumKey.Year,
                        Tracks = new List<TrackDto>()
                    };
                    currentAlbumKey = nextTrackAlbumKey;
                }
                currentAlbum.Tracks.Add(track.TrackDto);
            }
            if(currentAlbum is not null) yield return currentAlbum;
        }

        /// <summary>
        /// Gets a stream of the track with the given ID.
        /// MIME type will be determined from the file extension, as specified by the user configuration.
        /// </summary>
        /// <param name="id">Unique database ID of the track.</param>
        /// <returns>A stream of the media file with ID <paramref name="id"/></returns>
        [HttpGet("MediaById/{id}")]
        public async Task<IActionResult> GetMediaById([Required] Guid? id)
        {
            var track = await musicContext.AllTracks
                .Select(t => new
                {
                    t.Id,
                    t.FullPathToDirectory,
                    t.FileNameIncludingExtension
                })
                .SingleOrDefaultAsync(t => t.Id == id);

            if(track == null)
            {
                return NotFound();
            }

            var extension = Path.GetExtension(track.FileNameIncludingExtension).TrimStart('.');
            var completePath = new FilePath(track.FullPathToDirectory, track.FileNameIncludingExtension).CompletePath();
            var stream = new FileStream(completePath, FileMode.Open, FileAccess.Read);
            return new FileStreamResult(stream, config.MimeTypes[extension]);
        }
    }
}