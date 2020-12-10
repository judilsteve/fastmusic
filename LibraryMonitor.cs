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

         /// <summary>
         /// Checks the configured library locations for new/updated/deleted files,
         /// and updates the database accordingly
         /// </summary>
        public async Task SynchroniseDb(PerformContext context, CancellationToken cancellationToken)
        {
            // TODO Factor this into functions for each step
            // This will make it more readable, and allow updating in batches,
            // to reduce peak memory usage

            context.WriteLine("Starting update: Enumerating files");
            var lastDbUpdateTime = await musicContext.GetLastUpdateTime();
            context.WriteLine(lastDbUpdateTime.HasValue ? $"Last library update was at {lastDbUpdateTime.Value.ToLocalTime()}" : "Library has never been updated");
            // Set the last update time now
            // Otherwise, files that change between now and update completion
            // might not get flagged for update up in the next sync round
            await musicContext.SetLastUpdateTime(DateTime.UtcNow.ToUniversalTime());

            var filePaths = new List<string>();
            var filePatterns = configuration.MimeTypes.Keys
                .Select(ext => $"*.{ext}")
                .ToArray();
            foreach(var directory in configuration.LibraryLocations)
            {
                foreach(var filePattern in filePatterns)
                {
                    filePaths.AddRange(Directory.EnumerateFiles(directory, filePattern, SearchOption.AllDirectories));
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            if(lastDbUpdateTime.HasValue)
            {
                context.WriteLine("Cleaning up tracks no longer present on disk");
                // Delete old tracks
                var deletedTrackCount = await musicContext.AllTracks
                    .Where(t => !filePaths.Contains(t.FilePath))
                    .BatchDeleteAsync(cancellationToken); // TODO Make this transactional and use batches/progress
                context.WriteLine($"Deleted {deletedTrackCount} tracks");
            }

            var toAdd = new List<string>();
            var toUpdate = new List<string>();
            context.WriteLine("Scanning filesystem records to find new and updated files");
            var findNewProgress = context.WriteProgressBar("Finding new/updated files");
            var pathsScanned = 0;
            foreach(var filePath in filePaths)
            {
                var fileInfo = new FileInfo(filePath);
                if(lastDbUpdateTime is null || fileInfo.CreationTimeUtc > lastDbUpdateTime)
                {
                    toAdd.Add(filePath);
                }
                else if(lastDbUpdateTime is null || fileInfo.LastWriteTimeUtc > lastDbUpdateTime)
                {
                    toUpdate.Add(filePath);
                }
                cancellationToken.ThrowIfCancellationRequested();
                findNewProgress.SetValue((double)pathsScanned++ / filePaths.Count * 100.0);
            }
            findNewProgress.SetValue(100.0);
            context.WriteLine($"Need to scan {toAdd.Count} new files and {toUpdate.Count} updated files for tags");

            var newOrUpdated = new List<DbTrack>(toAdd.Count + toUpdate.Count);
            context.WriteLine($"Scanning {toAdd.Count} new files for tags");
            var toAddProgress = context.WriteProgressBar("Scanning new files");
            var toAddScanned = 0;
            foreach(var filePath in toAdd)
            {
                if(TryCreateDbTrack(filePath, out var track1, out var exceptionMessage))
                {
                    newOrUpdated.Add(track1!);
                }
                else
                {
                    // TODO: Hangfire WriteError and WriteWarning extensions
                    context.WriteLine($"Error reading tags of file \"{filePath}\". File will not be added to library. Details:");
                    context.WriteLine(exceptionMessage);
                }
                cancellationToken.ThrowIfCancellationRequested();
                toAddProgress.SetValue((double)toAddScanned++ / toAdd.Count * 100.0);
            }
            toAddProgress.SetValue(100.0);

            context.WriteLine("Fetching IDs of files that need to be updated");
            var toUpdateIds = await musicContext.AllTracks
                .Where(t => toUpdate.Contains(t.FilePath))
                .Select(t => new { t.FilePath, t.Id })
                // Avoids seeking back and forth across the drive between db and library
                .ToArrayAsync(cancellationToken);

            context.WriteLine($"Scanning {toUpdateIds.Length} updated files for tags");
            var toUpdateProgress = context.WriteProgressBar("Scanning updated files");
            var toUpdateScanned = 0;
            foreach(var toUpdateInfo in toUpdateIds)
            {
                if(TryCreateDbTrack(toUpdateInfo.FilePath, out var track, out var exceptionMessage, toUpdateInfo.Id))
                {
                    newOrUpdated.Add(track!);
                }
                else
                {
                    // TODO: Hangfire WriteError and WriteWarning extensions
                    context.WriteLine($"Error reading tags of file \"{toUpdateInfo.FilePath}\". Tags will not be updated in the library. Details:");
                    context.WriteLine(exceptionMessage);
                }
                cancellationToken.ThrowIfCancellationRequested();
                toUpdateProgress.SetValue((double)toUpdateScanned++ / toUpdateIds.Length * 100.0);
            }
            toUpdateProgress.SetValue(100.0);

            await musicContext.BulkInsertOrUpdateAsync(newOrUpdated, cancellationToken: cancellationToken); // TODO Make this transactional and use batches/progress
        }

        private static bool TryCreateDbTrack(string filePath, out DbTrack? track, out string? exceptionMessage, Guid? id = null)
        {
            TagLib.File tagFile;
            try
            {
                tagFile = TagLib.File.Create(filePath);
            }
            catch(Exception e)
            {
                track = null;
                exceptionMessage = e.Message;
                return false;
            }
            try
            {
                var tag = tagFile.Tag;
                track = new DbTrack
                {
                    FilePath = filePath,
                    Id = id ?? Guid.NewGuid()
                };
                track.SetTrackData(tag);
                exceptionMessage = null;
                return true;
            }
            finally
            {
                tagFile.Dispose();
            }
        }
    }
}