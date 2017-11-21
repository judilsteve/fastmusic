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
            : base(new DbContextOptions<MusicProvider>())
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={m_dbFileName}");
        }

        public DbSet<DbTrack> AllTracks { get; set; }

        private class DbUpdateTime
        {
            [Key]
            public int Id { get; set; }
            public DateTime UpdateTime { get; set; }
        }
        private DbSet<DbUpdateTime> LastUpdateTime { get; set; }

        /**
         * @note This does *NOT* call SaveChanges()
         */
        public void SetLastUpdateTime(DateTime newTime)
        {
            var oldTime = LastUpdateTime.SingleOrDefault();
            if(oldTime != null)
            {
                LastUpdateTime.Remove(oldTime);
            }
            LastUpdateTime.Add(new DbUpdateTime{ UpdateTime = newTime });
        }

        public DateTime GetLastUpdateTime()
        {
            var lastUpdateTime = LastUpdateTime.SingleOrDefault();
            return lastUpdateTime != null ? lastUpdateTime.UpdateTime : DateTime.MinValue.ToUniversalTime();
        }
    }
}