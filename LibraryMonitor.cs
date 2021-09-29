using fastmusic.DataProviders;
using fastmusic.DataTypes;
using fastmusic.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Server;
using Hangfire.Console;
using EFCore.BulkExtensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace fastmusic
{
    // TODO This class is a great candidate for unit tests
    /// <summary>
    /// Monitors the user-configured library directories at a user-configured interval,
    /// scanning them for new, updated, or deleted files.
    /// </summary>
    public class LibraryMonitor
    {
        private readonly Configuration configuration;
        private readonly MusicContext musicContext;

         /// <summary>
         /// Constructor
         /// </summary>
         /// <param name="configuration">Application configuration</param>
         /// <param name="musicContext">Provides access to music tables in database</param>
        public LibraryMonitor(
            Configuration configuration,
            MusicContext musicContext)
        {
            this.configuration = configuration;
            this.musicContext = musicContext;
        }

        private struct UpdatedTrackDto : IFilePath
        {
            public string? FullPathToDirectory { get; private init; }
            public string FileNameIncludingExtension { get; private init; }
            public readonly Guid Id;

            public UpdatedTrackDto(string? fullPathToDirectory, string fileNameIncludingExtension, Guid id)
            {
                FullPathToDirectory = fullPathToDirectory;
                FileNameIncludingExtension = fileNameIncludingExtension;
                Id = id;
            }
        }

         /// <summary>
         /// Checks the configured library locations for new/updated/deleted files,
         /// and updates the database accordingly
         /// </summary>
        public async Task SynchroniseDb(PerformContext context, CancellationToken cancellationToken)
        {
            // TODO Is it possible/beneficial to do some or all of this in batches?

            var lastDbUpdateTime = await musicContext.GetLastUpdateTime();
            context.WriteLine(lastDbUpdateTime.HasValue ? $"Last library update was at {lastDbUpdateTime.Value.ToLocalTime()}" : "Library has never been updated");
            // Set the last update time now
            // Otherwise, files that change between now and update completion
            // might not get flagged for update up in the next sync round
            await musicContext.SetLastUpdateTime(DateTime.UtcNow.ToUniversalTime());

            context.WriteLine("Starting update: Scanning filesystem to find new and updated tracks");
            var filePatterns = configuration.MimeTypes.Keys
                .Select(ext => $"*.{ext}")
                .ToArray();
            var status = EnumerateFiles(new FileSearch(lastDbUpdateTime, filePatterns, Array.Empty<string>()/*TODO*/), cancellationToken);
            var added = status.Tracks.Added;
            var updated = status.Tracks.Updated;
            var unchanged = status.Tracks.Unchanged;
            context.WriteLine($"Found {added.Count} new tracks, {updated.Count} updated tracks, and {unchanged.Count} unchanged tracks");

            var filePathsAlreadyInDb = unchanged.Concat(updated);
            if(lastDbUpdateTime.HasValue)
            {
                context.WriteLine("Finding and deleting database records for tracks no longer present on disk");
                // Delete old tracks
                var deletedTrackCount = await musicContext.AllTracks
                    .Where(t => !filePathsAlreadyInDb
                        .Any(a => t.FileNameIncludingExtension == a.FileNameIncludingExtension && t.FullPathToDirectory == a.FullPathToDirectory))
                    .BatchDeleteAsync(cancellationToken); // TODO Make this transactional and use batches/progress
                context.WriteLine($"Deleted {deletedTrackCount} orphaned tracks");
            }

            var newOrUpdated = new List<DbTrack>(added.Count + updated.Count);
            if(added.Count > 0)
                // TODO It is possible to create a duplicate track here:
                // 1. Wait for SetLastUpdateTime to complete
                // 2. Add a track before files get enumerated
                // 3. Track is added to the database
                // 4. On next update, track is considered new (because its creation time is later than lastDbUpdateTime), and added again
                // In general, this code needs to be audited for race conditions
                newOrUpdated.AddRange(BuildTracks(added, added.Count, context, cancellationToken));

            if(updated.Count > 0)
                newOrUpdated.AddRange(BuildUpdatedTracks(updated, context, cancellationToken));

            if(newOrUpdated.Count > 0)
            {
                context.WriteLine("Bulk upserting records");
                await musicContext.BulkInsertOrUpdateAsync(newOrUpdated, cancellationToken: cancellationToken); // TODO Make this transactional and use batches/progress
                context.WriteLine("Done");
            }
        }

        // NOTE: It is important to UpdateAlbumArt that these be in ascending order
        private static readonly int[] imageDimensions = new[]{ 192, 384 };

        private static readonly JpegEncoder jpegEncoder = new JpegEncoder
        {
            Quality = 92,
            Subsample = JpegSubsample.Ratio420
        };

        private async Task UpdateAlbumArt(DateTime? lastDbUpdateTime, LibraryStatus status, CancellationToken cancellationToken)
        {
            var added = status.Art.Added;

            var newArt = new List<DbArt>();
            foreach(var imagePath in added)
            {
                Image image;
                try
                {
                    // TODO Explore configuration options for this function
                    image = await Image.LoadAsync(imagePath.ToString(), cancellationToken);
                }
                catch(Exception)
                {
                    // TODO Error logging
                    continue;
                }
                try
                {
                    var portrait = image.Height > image.Width;
                    var originalDimension = portrait ? image.Height : image.Width;
                    var dbArt = new DbArt
                    {
                        Id = Guid.NewGuid(),
                        FullPathToDirectory = imagePath.FullPathToDirectory,
                        FileNameIncludingExtension = imagePath.FileNameIncludingExtension,
                        OriginalDimension = (uint)originalDimension
                    };
                    foreach(var dimension in imageDimensions)
                    {
                        if(dimension > originalDimension) break;
                        int newHeight, newWidth;
                        if(portrait)
                        {
                            newHeight = dimension;
                            newWidth = (int)Math.Round(dimension * image.Width / (double)image.Height);
                        }
                        else
                        {
                            newWidth = dimension;
                            newHeight = (int)Math.Round(dimension * image.Height / (double)image.Width);
                        }
                        var resized = image.Clone(i => i.Resize(newWidth, newHeight, sampler: KnownResamplers.Robidoux, compand: true));
                        // TODO WebP
                        await resized.SaveAsJpegAsync($"art/{dbArt.Id}_{dimension}.jpg", jpegEncoder, cancellationToken);
                    }
                    newArt.Add(dbArt);
                }
                finally
                {
                    image.Dispose();
                }
            }
            await musicContext.BulkInsertAsync(newArt, cancellationToken: cancellationToken);

            // TODO Edits and deletes
        }

        private struct FileSearch
        {
            public readonly DateTime? LastUpdateTime;
            public readonly string[] TrackFilePatterns;
            public readonly string[] ArtFilePatterns;

            public FileSearch(
                DateTime? lastUpdateTime,
                string[] trackFilePatterns,
                string[] artFilePatterns)
            {
                LastUpdateTime = lastUpdateTime;
                TrackFilePatterns = trackFilePatterns;
                ArtFilePatterns = artFilePatterns;
            }
        }

        private LibraryStatus EnumerateFiles(FileSearch fileSearch, CancellationToken cancellationToken)
        {
            var status = new LibraryStatus();
            foreach(var directory in configuration.LibraryLocations)
            {
                EnumerateFilesInner(fileSearch.ArtFilePatterns, directory, fileSearch.LastUpdateTime, ref status.Art, cancellationToken);
                EnumerateFilesInner(fileSearch.TrackFilePatterns, directory, fileSearch.LastUpdateTime, ref status.Tracks, cancellationToken);
            }
            return status;
        }

        private class LibraryStatus
        {
            public class FilesStatus
            {
                public readonly List<FilePath> Added = new();
                public readonly List<FilePath> Updated = new();
                public readonly List<FilePath> Unchanged = new();
            }

            public FilesStatus Tracks = new();
            public FilesStatus Art = new();
        }

        private void EnumerateFilesInner(
            IEnumerable<string> searchPatterns,
            string basePath,
            DateTime? lastUpdateTime,
            ref LibraryStatus.FilesStatus status,
            CancellationToken cancellationToken)
        {
            foreach(var searchPattern in searchPatterns)
            {
                foreach(var filePath in Directory.EnumerateFiles(basePath, searchPattern, SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(filePath);
                    var splitPath = new FilePath(fileInfo);
                    List<FilePath> destination;
                    if(lastUpdateTime is null || fileInfo.CreationTimeUtc > lastUpdateTime)
                        destination = status.Added;
                    else if(fileInfo.LastWriteTimeUtc > lastUpdateTime)
                        destination = status.Updated;
                    else
                        destination = status.Unchanged;
                    destination.Add(splitPath);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private IEnumerable<DbTrack> BuildUpdatedTracks(IEnumerable<FilePath> updated, PerformContext context, CancellationToken cancellationToken)
        {
            context.WriteLine("Fetching IDs of tracks that need to be updated");
            var toUpdateIds = musicContext.AllTracks
                .Select(t => new
                {
                    t.FullPathToDirectory,
                    t.FileNameIncludingExtension,
                    t.Id
                })
                .Where(t => updated
                    .Any(u => u.FileNameIncludingExtension == t.FileNameIncludingExtension
                        && u.FullPathToDirectory == t.FullPathToDirectory))
                .AsEnumerable()
                .Select(t => new UpdatedTrackDto(t.FullPathToDirectory, t.FileNameIncludingExtension, t.Id))
                // Avoids seeking back and forth across the drive between db and library
                .ToArray();
            cancellationToken.ThrowIfCancellationRequested();

            context.WriteLine($"Scanning {toUpdateIds.Length} updated tracks for tag info");
            var toUpdateProgress = context.WriteProgressBar("Scanning updated tracks");
            var toUpdateScanned = 0;
            foreach(var toUpdateInfo in toUpdateIds)
            {
                DbTrack? track;
                try
                {
                    track = CreateDbTrack(toUpdateInfo);
                    track.Id = toUpdateInfo.Id;
                }
                catch(Exception e)
                {
                    // TODO: Hangfire WriteError and WriteWarning extensions
                    context.WriteLine($"Error reading tags of file \"{toUpdateInfo.CompletePath()}\" Tags will not be updated in the library. Details:");
                    context.WriteLine(e.Message);
                    track = null;
                }
                if(track is not null) yield return track;
                cancellationToken.ThrowIfCancellationRequested();
                toUpdateProgress.SetValue((double)toUpdateScanned++ / toUpdateIds.Length * 100.0);
            }
            toUpdateProgress.SetValue(100.0);
        }

        private IEnumerable<DbTrack> BuildTracks(IEnumerable<FilePath> added, int addedCount, PerformContext context, CancellationToken cancellationToken)
        {
            context.WriteLine($"Scanning {addedCount} new files for tags");
            var toAddProgress = context.WriteProgressBar("Scanning new tracks");
            var toAddScanned = 0;
            foreach(var filePath in added)
            {
                DbTrack? track;
                try
                {
                    track = CreateDbTrack(filePath);
                    track.Id = Guid.NewGuid();
                }
                catch(Exception e)
                {
                    // TODO: Hangfire WriteError and WriteWarning extensions
                    context.WriteLine($"Error reading tags of file \"{filePath}\". File will not be added to library. Details:");
                    context.WriteLine(e.Message);
                    track = null;
                }
                if(track is not null) yield return track;
                cancellationToken.ThrowIfCancellationRequested();
                toAddProgress.SetValue((double)toAddScanned++ / addedCount * 100.0);
            }
            toAddProgress.SetValue(100.0);
        }

        private class TagReadException : Exception
        {
            public TagReadException(IFilePath filePath, Exception inner) : base($"Failed to read tags for \"{filePath.CompletePath()}\"", inner) {}
        }

        private static DbTrack CreateDbTrack(IFilePath filePath)
        {
            TagLib.File tagFile;
            try
            {
                tagFile = TagLib.File.Create(filePath.ToString());
            }
            catch(Exception e)
            {
                throw new TagReadException(filePath, e);
            }
            try
            {
                var track = new DbTrack
                {
                    FullPathToDirectory = filePath.FullPathToDirectory,
                    FileNameIncludingExtension = filePath.FileNameIncludingExtension
                };
                track.SetTrackData(tagFile.Tag);
                return track;
            }
            finally
            {
                tagFile.Dispose();
            }
        }
    }
}