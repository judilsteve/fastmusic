using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fastmusic.DataTypes
{

    /// <summary>
    /// Database representation of an album art image
    /// </summary>
    public class DbAlbumArt
    {
        /// <summary>
        /// Unique identifier for this album art
        /// Used as the filename for the scaled versions
        /// </summary>
        [Key]
        public string Id { get; set; }

        /// <summary>
        /// Full file path to the original album art
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// All album art imported into the database is copied from its original location
        /// into new images of various sizes (the set of sizes is user-configurable).
        /// Album art will only be stored in sizes for which the original file's resolution
        /// is greater than or equal to that size. E.g. if the user specifies that album art
        /// should be stored in 128, 256, and 512 pixel forms, then an album art file with
        /// dimensions 400 by 400 will only be stored in 128 and 256 pixel forms.
        /// Album art size is specified by the largest dimension only. The smaller dimension
        /// will be determined by the aspect ratio of the original file (hopefully 1:1).
        /// </summary>
        public uint MaxWidth { get; set; }

        /// <summary>
        /// List of tracks that have this album art.
        /// Note: This is a virtual column used to establish an implicit one-to-many
        /// relationship between DbAlbumArt and DbTrack.
        /// </summary>
        public DbTrack Tracks { get; set; }
    }
}