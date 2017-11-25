using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using fastmusic.DataTypes;

namespace fastmusic.DataProviders
{
     /// <summary>
     /// Provides track metadata
     /// </summary>
    public class MusicProvider : DbContext
    {
        /// <summary>
        /// File name of the SQLite database
        /// </summary>
        private const string m_dbFileName = "fastmusic.db";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="options">Options for the MusicProvider. Note that some of these may be overridden, see OnConfiguring.</param>
        public MusicProvider(DbContextOptions<MusicProvider> options)
            : base(options)
        {
        }

        /// <summary>
        /// Constructor that uses default options.
        /// </summary>
        public MusicProvider()
            : base(new DbContextOptions<MusicProvider>())
        {
        }

        /// <summary>
        /// Modifies configuration, will override constructor-provided values.
        /// </summary>
        /// <param name="optionsBuilder">Object that allows setting options for this MusicProvider.</param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={m_dbFileName}");
        }

        /// <summary>
        /// All track metadata in the library
        /// </summary>
        public DbSet<DbTrack> AllTracks { get; set; }

        /// <summary>
        /// Database representation of a time at which a sync began.
        /// </summary>
        private class DbUpdateTime
        {
            /// <summary>
            /// Auto-assigned key, required by EF Core.
            /// </summary>
            [Key]
            public int Id { get; set; }

            /// <summary>
            /// Time at which this disk-to-database sync began.
            /// </summary>
            public DateTime UpdateTime { get; set; }
        }

        /// <summary>
        /// Holds the last disk-to-database sync time.
        /// Should only ever have one element.
        /// </summary>
        private DbSet<DbUpdateTime> LastUpdateTime { get; set; }

         /// <summary>
         /// Call this at the beginning of every disk-to-database sync.
         /// @note Calls SaveChanges
         /// </summary>
         /// <param name="newTime">Time at which this disk-to-database sync began.</param>
        public async Task SetLastUpdateTime(DateTime newTime)
        {
            var oldTime = LastUpdateTime.SingleOrDefault();
            if(oldTime != null)
            {
                LastUpdateTime.Remove(oldTime);
            }
            LastUpdateTime.Add(new DbUpdateTime{ UpdateTime = newTime });
            await SaveChangesAsync();
        }

        /// <returns>The last time a disk-to-database sync began.</returns>
        public DateTime GetLastUpdateTime()
        {
            var lastUpdateTime = LastUpdateTime.SingleOrDefault();
            return lastUpdateTime != null ? lastUpdateTime.UpdateTime : DateTime.MinValue.ToUniversalTime();
        }
    }
}