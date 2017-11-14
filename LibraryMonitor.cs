using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using fastmusic.DataProviders;
using fastmusic.DataTypes;

namespace fastmusic
{
    // TODO This class is a great candidate for unit tests
    public class LibraryMonitor
    {
        private static LibraryMonitor m_instance;

        private List<FileSystemWatcher> m_monitors = new List<FileSystemWatcher>();

        private List<String> m_libraryLocations;
        private List<String> m_fileTypes;

        private Timer m_syncTimer;

        private const int SAVE_TO_DISK_INTERVAL = 512;

        public static LibraryMonitor GetInstance(List<String> libraryLocations, List<String> fileTypes)
        {
            if(m_instance == null) m_instance = new LibraryMonitor(libraryLocations, fileTypes);
            return m_instance;
        }

        private LibraryMonitor(List<String> libraryLocations, List<String> fileTypes)
        {
            m_libraryLocations = libraryLocations;
            m_fileTypes = fileTypes;

            foreach(var libraryLocation in m_libraryLocations)
            {
                // FileSystemWatcher doesn't support watching multiple file patterns
                // A separate watcher object must be created for each file type
                foreach(var fileType in m_fileTypes)
                {
                    var libMon = new FileSystemWatcher(libraryLocation,  $"*.{fileType}");
                    libMon.Changed += new FileSystemEventHandler(async (obj, e) => await UpdateTrack(e.FullPath));
                    libMon.Created += new FileSystemEventHandler(async (obj, e) => await AddTrack(e.FullPath));
                    libMon.Deleted += new FileSystemEventHandler(async (obj, e) => await RemoveTrack(e.FullPath));
                    libMon.Renamed += new RenamedEventHandler(async (obj, e) => await UpdateTrackFileName(e.OldFullPath, e.FullPath));
                    libMon.Renamed += new RenamedEventHandler(async (obj, e) => await UpdateTrackFileName(e.OldFullPath, e.FullPath));
                    libMon.EnableRaisingEvents = true;
                    m_monitors.Add(libMon);
                }
            }

            Console.Out.WriteLine("LibraryMonitor: Registered library monitors.");

            // Apparently FileSystemWatcher isn't 100% reliable
            // Schedule a task to synchronise library and db every so often
            m_syncTimer = new Timer(async (o) => await m_instance.SynchroniseDb(), null, 0, 2 * 60 * 1000);
            Console.Out.WriteLine("LibraryMonitor: Set up synchronisation routine.");
        }

        public async Task SynchroniseDb()
        {
            await Console.Out.WriteLineAsync("SynchroniseDb: Starting update (enumerating files).");
            var filesToBeUpdated = new List<String>();
            foreach(var libraryLocation in m_libraryLocations)
            {
                // FileSystemWatcher doesn't support watching multiple file patterns
                // A separate watcher object must be created for each file type
                foreach(var fileType in m_fileTypes)
                {
                    using(MusicProvider mp = new MusicProvider())
                    {
                        EnumerateFilesMatchingPredicate(
                            startDirectory: libraryLocation,
                            searchPattern: $"*.{fileType}",
                            predicate: f => DoesFileNeedUpdate(f, mp),
                            filesMatchingPredicate: ref filesToBeUpdated
                        );
                    }
                }
            }
            await Console.Out.WriteLineAsync($"SynchroniseDb: {filesToBeUpdated.Count} files need to be updated.");
            foreach(var file in filesToBeUpdated)
            {
                await Console.Out.WriteLineAsync(file);
            }
            await UpdateFiles(filesToBeUpdated);
            await Console.Out.WriteLineAsync("SynchroniseDb: Finished updating.");

            // Delete tracks no longer present on disk from the DB
            IQueryable<DbTrack> tracksToDelete;
            using(MusicProvider mp = new MusicProvider())
            {
                tracksToDelete = mp.AllTracks.Where( t =>
                    !File.Exists(t.FileName)
                );

                var i = 0;
                foreach(var track in tracksToDelete)
                {
                    await Console.Out.WriteLineAsync($"LibraryMonitor: Removing: {track.FileName}.");
                    mp.AllTracks.Remove(track);
                    if(i++ % SAVE_TO_DISK_INTERVAL == 0)
                    {
                        await mp.SaveChangesAsync();
                    }
                }
                await mp.SaveChangesAsync();
            }
        }

        private bool DoesFileNeedUpdate(string fileName, MusicProvider mp)
        {
            if(!mp.AllTracks.Any( t => t.FileName == fileName))
            {
                // This file is not in the database at all
                return true;
            }
            else if(new FileInfo(fileName).LastWriteTime.ToUniversalTime() > mp.AllTracks.First( t => t.FileName == fileName).LastUpdateTime)
            {
                // Last write time is newer than last db update time
                return true;
            }
            else return false;
        }

        /**
         * Not sure if Directory.EnumerateFiles uses deferred execution
         * Rolling our own to ensure best performance
         * Ref param used to avoid allocating a new list on each recursion
         */
        // TODO Split into files that need updates and files that need to be added to speed up UpdateFiles (currently it has a redundant check)
        private void EnumerateFilesMatchingPredicate(
            string startDirectory,
            string searchPattern,
            Func<string, bool> predicate,
            ref List<string> filesMatchingPredicate
        )
        {
            foreach(var subDir in Directory.EnumerateDirectories(startDirectory))
            {
                EnumerateFilesMatchingPredicate(subDir, searchPattern, predicate, ref filesMatchingPredicate);
            }
            filesMatchingPredicate.AddRange(
                Directory.EnumerateFiles(startDirectory, searchPattern, SearchOption.TopDirectoryOnly)
                .Where( f => predicate(f) )
            );
        }

        private async Task UpdateFiles(List<String> filesToUpdate)
        {
        using(MusicProvider mp = new MusicProvider())
        {
            int i = 0; // Used to periodically save changes to the db
            foreach(var trackFileName in filesToUpdate)
            {
                if(await mp.AllTracks.AnyAsync(t => t.FileName == trackFileName))
                {
                    // Track already exists in the database
                    var trackToUpdate = await mp.AllTracks.FirstAsync( t =>
                        t.FileName == trackFileName
                    );

                    var tag = TagLib.File.Create(trackFileName).Tag;
                    // Update the track
                    await Console.Out.WriteLineAsync($"LibraryMonitor: Updating: {trackFileName}.");
                    trackToUpdate.SetTrackData(tag);
                    mp.AllTracks.Update(trackToUpdate);
                }
                else
                {
                    await Console.Out.WriteLineAsync($"LibraryMonitor: Adding: {trackFileName}.");
                    // New track. Add to database
                    var newTrack = new DbTrack{
                        FileName = trackFileName
                    };
                    newTrack.SetTrackData(TagLib.File.Create(trackFileName).Tag);
                    await mp.AllTracks.AddAsync(newTrack);
                }

                if(i++ % SAVE_TO_DISK_INTERVAL == 0)
                {
                    await mp.SaveChangesAsync();
                }
            }
            await mp.SaveChangesAsync();
        }
        }

        private async Task UpdateTrack(string trackFileName)
        {
        using(MusicProvider mp = new MusicProvider())
        {

            if(!(await mp.AllTracks.AnyAsync( t => t.FileName == trackFileName )))
            {
                await Console.Error.WriteLineAsync("LibraryMonitor: Received update event for track that was not in the database");
                await AddTrack(trackFileName);
            }

            await Console.Out.WriteLineAsync($"LibraryMonitor: Updating: {trackFileName}.");

            var trackToUpdate = await mp.AllTracks.FirstAsync( t =>
                t.FileName == trackFileName
            );

            trackToUpdate.SetTrackData(TagLib.File.Create(trackFileName).Tag);

            mp.AllTracks.Update(trackToUpdate);

            await mp.SaveChangesAsync();

            }
        }

        private async Task AddTrack(string trackFileName)
        {
            using(MusicProvider mp = new MusicProvider())
            {

            if(await mp.AllTracks.AnyAsync( t => t.FileName == trackFileName ))
            {
                await Console.Error.WriteLineAsync("LibraryMonitor: Received creation event for track that was already in the database");
                await UpdateTrack(trackFileName);
            }

            await Console.Out.WriteLineAsync($"LibraryMonitor: Adding: {trackFileName}.");

            var newTrack = new DbTrack();

            newTrack.SetTrackData(TagLib.File.Create(trackFileName).Tag);

            await mp.AllTracks.AddAsync(newTrack);

            await mp.SaveChangesAsync();

        }
        }

        private async Task RemoveTrack(string trackFileName)
        {
        using(MusicProvider mp = new MusicProvider())
        {

            if(!(await mp.AllTracks.AnyAsync( t => t.FileName == trackFileName )))
            {
                await Console.Error.WriteLineAsync("LibraryMonitor: Received deletion event for track that was not in the database");
                return;
            }

            await Console.Out.WriteLineAsync($"LibraryMonitor: Removing: {trackFileName}.");

            var trackToDelete = await mp.AllTracks.FirstAsync( t =>
                t.FileName == trackFileName
            );

            mp.AllTracks.Remove(trackToDelete);

            await mp.SaveChangesAsync();

        }
        }

        private async Task UpdateTrackFileName(string oldFileName, string newFileName)
        {
        using(MusicProvider mp = new MusicProvider())
        {

            if(!(await mp.AllTracks.AnyAsync( t => t.FileName == oldFileName )))
            {
                await Console.Error.WriteLineAsync("LibraryMonitor: Received update filename event for track that was not in the database");
                await AddTrack(newFileName);
            }

            await Console.Out.WriteLineAsync($"LibraryMonitor: Updating filename: \"{oldFileName}\" -> \"{newFileName}\".");

            var trackToUpdate = await mp.AllTracks.FirstAsync( t =>
                t.FileName == oldFileName
            );

            trackToUpdate.FileName = newFileName;

            mp.AllTracks.Update(trackToUpdate);

            await mp.SaveChangesAsync();

        }
        }
    }
}