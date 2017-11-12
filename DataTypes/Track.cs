using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TagLib;

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

        public bool HasSameData(Tag tag)
        {
            return
            Title == tag.Title &&
            Album == tag.Album &&
            AlbumArtist == GetAlbumArtist(tag) &&
            Performer == GetPerformer(tag) &&
            TrackNumber == tag.Track &&
            Year == tag.Year;
        }

        public void SetTrackData(Tag tag)
        {
            Title = tag.Title;
            TrackNumber = tag.Track;
            Album = tag.Album;
            AlbumArtist = GetAlbumArtist(tag);
            Performer = GetPerformer(tag);
            Year = tag.Year;
        }

        private string GetAlbumArtist(Tag tag)
        {
            return tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : tag.Performers.Length > 0 ? tag.Performers[0] : null;
        }

        private string GetPerformer(Tag tag)
        {
            return tag.Performers.Length > 0 ? tag.Performers[0] :tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : null;
        }
    }
}