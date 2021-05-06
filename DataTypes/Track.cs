using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TagLib;

namespace fastmusic.DataTypes
{
     /// <summary>
     /// Database representation of a single track and its metadata
     /// </summary>
    public class DbTrack : DbFile
    {
        /// <summary>
        /// Unique ID of this track.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)] public Guid Id { get; set; }

        /// <summary>
        /// Title of this track.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Name of the album this track belongs to.
        /// </summary>
        public string? Album { get; set; }

        /// <summary>
        /// Name of the artist that released the album this track belongs to.
        /// </summary>
        public string? AlbumArtist { get; set; }

        /// <summary>
        /// Name of the artist who performed this track.
        /// </summary>
        public string? Performer { get; set; }

        /// <summary>
        /// Number of this track in its album.
        /// </summary>
        public uint? TrackNumber { get; set; }

        /// <summary>
        /// Year of release for this track.
        /// </summary>
        public uint? Year { get; set; }

        /// <summary>
        /// Sets this track's metadata to be the same as that of <paramref name="tag"/>.
        /// </summary>
        /// <param name="tag">An ID3 tag of track metadata.</param>
        public void SetTrackData(Tag tag)
        {
            Title = tag.Title;
            TrackNumber = tag.Track;
            Album = tag.Album;
            AlbumArtist = GetAlbumArtist(tag);
            Performer = GetPerformer(tag);
            Year = tag.Year;
        }

        /// <param name="tag">An ID3 tag of track metadata.</param>
        /// <returns>The artist that released the album <paramref name="tag"/> is from. Falls back to track performer if necessary.</returns>
        private string? GetAlbumArtist(Tag tag)
        {
            return tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : tag.Performers.Length > 0 ? tag.Performers[0] : null;
        }

        /// <param name="tag">An ID3 tag of track metadata.</param>
        /// <returns>The artist that performed the track <paramref name="tag"/>. Falls back to album artist if necessary.</returns>
        private string? GetPerformer(Tag tag)
        {
            return tag.Performers.Length > 0 ? tag.Performers[0] :tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : null;
        }
    }
}