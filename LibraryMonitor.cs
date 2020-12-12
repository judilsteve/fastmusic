using fastmusic.DataProviders;
using fastmusic.DataTypes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.Server;
using Hangfire.Console;
using EFCore.BulkExtensions;

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

        private struct UpdatedTrackDto
        {
            public readonly string FilePath;
            public readonly Guid Id;

            public UpdatedTrackDto(string filePath, Guid id)
            {
                FilePath = filePath;
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
            var (added, updated, unchanged) = EnumerateFiles(lastDbUpdateTime, cancellationToken);
            context.WriteLine($"Found {added.Count} new tracks, {updated.Count} updated tracks, and {unchanged.Count} unchanged tracks");

            var filePathsAlreadyInDb = unchanged.Concat(updated);
            if(lastDbUpdateTime.HasValue)
            {
                context.WriteLine("Finding and deleting database records for tracks no longer present on disk");
                // Delete old tracks
                var deletedTrackCount = await musicContext.AllTracks
                    .Where(t => !filePathsAlreadyInDb.Contains(t.FilePath))
                    .BatchDeleteAsync(cancellationToken); // TODO Make this transactional and use batches/progress
                context.WriteLine($"Deleted {deletedTrackCount} orphaned tracks");
            }

            var newOrUpdated = new List<DbTrack>(added.Count + updated.Count);
            if(added.Count > 0)
                newOrUpdated.AddRange(BuildTracks(added, context, cancellationToken));

            if(updated.Count > 0)
                newOrUpdated.AddRange(BuildUpdatedTracks(updated, context, cancellationToken));

            if(newOrUpdated.Count > 0)
            {
                context.WriteLine("Bulk upserting records");
                await musicContext.BulkInsertOrUpdateAsync(newOrUpdated, cancellationToken: cancellationToken); // TODO Make this transactional and use batches/progress
                context.WriteLine("Done");
            }
        }

        private (List<string> added, List<string> updated, List<string> unchanged) EnumerateFiles(DateTime? lastDbUpdateTime, CancellationToken cancellationToken)
        {
            var added = new List<string>();
            var updated = new List<string>();
            var unchanged = new List<string>();
            var filePatterns = configuration.MimeTypes.Keys
                .Select(ext => $"*.{ext}");
            foreach(var directory in configuration.LibraryLocations)
            {
                foreach(var filePattern in filePatterns)
                {
                    foreach(var filePath in Directory.EnumerateFiles(directory, filePattern, SearchOption.AllDirectories))
                    {
                        var fileInfo = new FileInfo(filePath);
                        if(lastDbUpdateTime is null || fileInfo.CreationTimeUtc > lastDbUpdateTime)
                            added.Add(filePath);
                        else if(fileInfo.LastWriteTimeUtc > lastDbUpdateTime)
                            updated.Add(filePath);
                        else
                            unchanged.Add(filePath);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
            return (added, updated, unchanged);
        }

        private IEnumerable<DbTrack> BuildUpdatedTracks(List<string> updated, PerformContext context, CancellationToken cancellationToken)
        {
            context.WriteLine("Fetching IDs of tracks that need to be updated");
            var toUpdateIds = musicContext.AllTracks
                .Where(t => updated.Contains(t.FilePath))
                .Select(t => new { t.FilePath, t.Id })
                .AsEnumerable()
                .Select(t => new UpdatedTrackDto(t.FilePath, t.Id))
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
                    track = CreateDbTrack(toUpdateInfo.FilePath);
                    track.Id = toUpdateInfo.Id;
                }
                catch(Exception e)
                {
                    // TODO: Hangfire WriteError and WriteWarning extensions
                    context.WriteLine($"Error reading tags of file \"{toUpdateInfo.FilePath}\". Tags will not be updated in the library. Details:");
                    context.WriteLine(e.Message);
                    track = null;
                }
                if(track is not null) yield return track;
                cancellationToken.ThrowIfCancellationRequested();
                toUpdateProgress.SetValue((double)toUpdateScanned++ / toUpdateIds.Length * 100.0);
            }
            toUpdateProgress.SetValue(100.0);
        }

        private IEnumerable<DbTrack> BuildTracks(List<string> added, PerformContext context, CancellationToken cancellationToken)
        {
            context.WriteLine($"Scanning {added.Count} new files for tags");
            var toAddProgress = context.WriteProgressBar("Scanning new tracks");
            var toAddScanned = 0;
            foreach(var filePath in added)
            {
                DbTrack? track;
                try
                {
                    track = CreateDbTrack(filePath);
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
                toAddProgress.SetValue((double)toAddScanned++ / added.Count * 100.0);
            }
            toAddProgress.SetValue(100.0);
        }

        private class TagReadException : Exception
        {
            public TagReadException(string filePath, Exception inner) : base($"Failed to read tags for \"{filePath}\"", inner) {}
        }

        private static DbTrack CreateDbTrack(string filePath)
        {
            TagLib.File tagFile;
            try
            {
                tagFile = TagLib.File.Create(filePath);
            }
            catch(Exception e)
            {
                throw new TagReadException(filePath, e);
            }
            try
            {
                var track = new DbTrack
                {
                    FilePath = filePath
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