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
        public AlbumArtProvider(DbContextOptions<MediaTypeProvider> options)
            : base(options)
        {
        }

        public AlbumArtProvider()
            : base(new DbContextOptions<AlbumArtProvider>())
        {
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(m_dbFileName);
        }

        public DbSet<DbAlbumArt> Art { get; set; }
    }
}