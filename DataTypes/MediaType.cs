using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fastmusic.DataTypes
{
    /**
     * Database representation of a media type
     */
    public class DbMediaType
    {
        [Key]
        public string Extension { get; set; }

        public string MimeType { get; set; }
    }
}