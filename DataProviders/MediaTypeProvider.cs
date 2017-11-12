using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using fastmusic.DataTypes;

namespace fastmusic.DataProviders
{
    /**
     * Provides mime types
     */
    public class MediaTypeProvider : DbContext
    {
        public const string m_dbName = "MediaTypes";

        public MediaTypeProvider(DbContextOptions<MediaTypeProvider> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(m_dbName);
        }

        public DbSet<DbMediaType> Types { get; set; }
    }
}