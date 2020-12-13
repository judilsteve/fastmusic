using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace fastmusic.DataTypes
{
     /// <summary>
     /// Database representation of a single piece of album art
     /// </summary>
    public class DbArt
    {
        /// <summary>
        /// Unique ID of this artwork.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)] public Guid Id { get; set; }

        /// <summary>
        /// Full file path to the image file in the library.
        /// </summary>
        public string FilePath { get; set; } = null!;

        /// <summary>
        /// Size (in pixels) of the largest dimension of the original image
        /// </summary>
        /// <value></value>
        public uint OriginalDimension { get; set; }
    }
}