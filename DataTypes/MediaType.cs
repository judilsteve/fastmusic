using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fastmusic.DataTypes
{
     /// <summary>
     /// Database representation of a file format/codec
     /// </summary>
    public class DbMediaType
    {
        /// <summary>
        /// File extension of the media type
        /// </summary>
        [Key]
        public string Extension { get; set; }


        /// <summary>
        /// Mime type that should be used to stream the media type
        /// </summary>
        public string MimeType { get; set; }
    }
}