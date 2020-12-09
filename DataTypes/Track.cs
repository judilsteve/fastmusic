using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using TagLib;

namespace fastmusic.DataTypes
{
     /// <summary>
     /// Database representation of a single track and its metadata
     /// </summary>
    public class DbTrack
    {
        /// <summary>
        /// Unique ID of this track. Autogenerated by the database.
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)] public Guid Id { get; set; }

        /// <summary>
        /// Title of this track.
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Full file path to this track in the library.
        /// </summary>
        public string FilePath { get; set; } = null!;

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

        /// <returns>A string representation of this track's metadata.</returns>
        public override string ToString()
        {
            if(!Title.IsNullOrEmpty())
            {
                if(TrackNumber.HasValue)
                    return $"{TrackNumber} - {Title}";
                else
                    return Title!;
            }
            else return Path.GetFileNameWithoutExtension(FilePath);
        }

        /// <param name="tag">An ID3 tag to compare this track's metadata to.</param>
        /// <returns>True iff. this track has the same metadata as @param tag.</returns>
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

        /// <summary>
        /// Sets this track's metadata to be the same as that of @param tag.
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
        /// <returns>The artist that released the album @param tag is from. Falls back to track performer if necessary.</returns>
        private string? GetAlbumArtist(Tag tag)
        {
            return tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : tag.Performers.Length > 0 ? tag.Performers[0] : null;
        }

        /// <param name="tag">An ID3 tag of track metadata.</param>
        /// <returns>The artist that performed the track @param tag. Falls back to album artist if necessary.</returns>
        private string? GetPerformer(Tag tag)
        {
            return tag.Performers.Length > 0 ? tag.Performers[0] :tag.AlbumArtists.Length > 0 ? tag.AlbumArtists[0] : null;
        }
    }
}