using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using fastmusic.DataTypes;

namespace fastmusic.DataProviders
{
    /// <summary>
    /// Maps track IDs to album art files
    /// </summary>
    public class AlbumArtProvider : DbContext
    {
        private const string m_dbFileName = "fastmusic.db"; // TODO Move this constant to a common area
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">Options for the MusicProvider. Note that some of these may be overridden, see OnConfiguring.</param>
        public AlbumArtProvider(DbContextOptions<AlbumArtProvider> options)
            : base(options)
        {
        }

        /// <summary>
        /// Constructor that uses default options.
        /// </summary>
        public AlbumArtProvider()
            : base(new DbContextOptions<AlbumArtProvider>())
        {
        }


        /// <summary>
        /// Modifies configuration, will override constructor-provided values.
        /// </summary>
        /// <param name="optionsBuilder">Object that allows setting options for this MusicProvider.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(m_dbFileName);
        }

        /// <summary>
        /// Database table of album art files
        /// </summary>
        public DbSet<DbAlbumArt> Art { get; set; }
    }
}