using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using fastmusic.DataTypes;

namespace fastmusic.DataProviders
{
    /**
     * Provides information required to build a deck
     * This includes who's swiped on who (and in which direction),
     * and user locations
     */
    public class MusicProvider : DbContext
    {
        private const string m_dbFileName = "fastmusic.db";
        public MusicProvider(DbContextOptions<MusicProvider> options)
            : base(options)
        {
        }

        public MusicProvider()
            : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={m_dbFileName}");
        }

        public void UpdateDb()
        {
            string[] libraryDirectories = {@"V:\Music (Organised - Lossless)\", @"V:\Music (Organised - Lossy)\"};
            string[] fileTypes = {"flac", "m4a", "mp3"};

            var musicFileNames = new List<String>();

            foreach(var libraryDirectory in libraryDirectories)
            {
                foreach(var fileType in fileTypes)
                {
                    musicFileNames.AddRange(Directory.EnumerateFiles(libraryDirectory, $"*.{fileType}", SearchOption.AllDirectories));
                }
            }

            UpdateTracks(musicFileNames);
        }

        private async void UpdateTracks(List<String> musicFileNames)
        {
            long id = 0;

            foreach(var musicFileName in musicFileNames)
            {
                var tag = TagLib.File.Create(musicFileName).Tag;

                var track = new DbTrack {
                    FileName = musicFileName,
                    Title = tag.Title,
                    TrackNumber = tag.Track,
                    Album = tag.Album,
                    AlbumArtist = tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : tag.Performers.Length > 0 ? tag.Performers[0] : null,
                    Performer = tag.Performers.Length > 0 ? tag.Performers[0] : tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : null,
                    Year = tag.Year
                };

                if(await AllTracks.AnyAsync(t => t.FileName == track.FileName))
                {
                    // Track already exists in the database, update it
                    DbTrack existingTrack = await AllTracks.FirstAsync(t => t.FileName == track.FileName);
                    if(!track.HasSameData(existingTrack))
                    {
                        existingTrack = track;
                        await Console.Out.WriteLineAsync($"Updating Track: {track}");
                    }
                }
                else
                {
                    // Unseen track. Add to database
                    await AllTracks.AddAsync(track);
                    await Console.Out.WriteLineAsync($"Adding Track: {track}");
                }

                id++;
                if(id % 4096 == 0)
                {
                    SaveChanges();
                }
            }

            SaveChanges();
        }

        public DbSet<DbTrack> AllTracks { get; set; }
    }
}