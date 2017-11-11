using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fastmusic.DataTypes
{
    /**
     * Database representation of a single track
     */
    public class DbTrack
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public string Title { get; set; }

        public string FileName { get; set; }

        public string Album { get; set; }

        public string AlbumArtist { get; set; }

        public string Performer { get; set; }

        public uint TrackNumber { get; set; }

        public uint Year { get; set; }

        public override string ToString()
        {
            return $"{TrackNumber} - {Title}";
        }

        public bool HasSameData(DbTrack other)
        {
            return
            Title == other.Title &&
            FileName == other.FileName &&
            Album == other.Album &&
            AlbumArtist == other.AlbumArtist &&
            Performer == other.Performer &&
            TrackNumber == other.TrackNumber &&
            Year == other.Year;
        }
    }
}