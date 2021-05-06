using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace fastmusic.DataTypes
{
     /// <summary>
     /// Database representation of a single piece of album art
     /// </summary>
    public class DbArt : DbFile
    {
        /// <summary>
        /// Unique ID of this artwork.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)] public Guid Id { get; set; }

        /// <summary>
        /// Size (in pixels) of the largest dimension of the original image
        /// </summary>
        /// <value></value>
        public uint OriginalDimension { get; set; }
    }
}