using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using fastmusic.DataTypes;

namespace fastmusic.DataProviders
{
    /**
     * Provides information about tracks
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

        public DbSet<DbTrack> AllTracks { get; set; }
    }
}