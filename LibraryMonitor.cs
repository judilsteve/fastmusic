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

        private List<string> m_libraryLocations;
        private List<string> m_fileTypes;
        private List<string> m_filePatterns = new List<string>();
        private HashSet<string> m_filesToUpdate = new HashSet<string>();
        private List<string> m_filesToAdd = new List<string>();

        private Timer m_syncTimer;

        private const int SYNC_INTERVAL_SECONDS = 120; // TODO Make this user configurable

        private const int SAVE_TO_DISK_INTERVAL = 2048;

        public static LibraryMonitor GetInstance(List<String> libraryLocations, List<String> fileTypes)
        {
            if(m_instance == null) m_instance = new LibraryMonitor(libraryLocations, fileTypes);
            return m_instance;
        }

        private LibraryMonitor(List<String> libraryLocations, List<String> fileTypes)
        {
            m_libraryLocations = libraryLocations;
            m_fileTypes = fileTypes;
            m_filePatterns = m_fileTypes.Select( fileType =>
                $"*.{fileType}"
            ).ToList();

            // Schedule a task to synchronise filesystem and DB every so often
            // We use Timeout.Infinite to avoid multiple syncs running concurrently
            // This is changed at the end of SynchroniseDb
            m_syncTimer = new Timer(async (o) => await SynchroniseDb(), null, 0, Timeout.Infinite);
        }

        public async Task SynchroniseDb()
        {
            await Console.Out.WriteLineAsync("LibraryMonitor: Starting update (enumerating files).");
            using(MusicProvider mp = new MusicProvider())
            {
                var lastDBUpdateTime = mp.GetLastUpdateTime();
                foreach(var libraryLocation in m_libraryLocations)
                {
                    FindFilesToUpdate(libraryLocation, mp, lastDBUpdateTime);
                }
            }

            await Console.Out.WriteLineAsync($"LibraryMonitor: {m_filesToUpdate.Count} files need to be updated and {m_filesToAdd.Count} files need to be added.");
            await UpdateFiles();
            m_filesToUpdate.Clear();
            await AddNewFiles();
            m_filesToAdd.Clear();
            await DeleteStaleDbEntries();
            await Console.Out.WriteLineAsync("LibraryMonitor: Database update completed successfully.");

            // Schedule the next sync
            m_syncTimer.Change(SYNC_INTERVAL_SECONDS * 1000, Timeout.Infinite);
        }

        private void FindFilesToUpdate(
            string startDirectory,
            MusicProvider mp,
            DateTime lastDBUpdateTime
        )
        {
            foreach(var subDir in Directory.EnumerateDirectories(startDirectory))
            {
                if(new DirectoryInfo(subDir).LastWriteTime > lastDBUpdateTime)
                {
                    FindFilesToUpdate(subDir, mp, lastDBUpdateTime);
                }
            }
            foreach(var filePattern in m_filePatterns)
            {
                foreach(var file in Directory.EnumerateFiles(startDirectory, filePattern, SearchOption.TopDirectoryOnly))
                {
                    if(!mp.AllTracks.AsNoTracking().Any( t => t.FileName == file ))
                    {
                        m_filesToAdd.Add(file);
                    }
                    else if(new FileInfo(file).LastWriteTime.ToUniversalTime() > lastDBUpdateTime)
                    {
                        m_filesToUpdate.Add(file);
                    }
                }
            }
        }

        private struct TrackToUpdate {
            public TagLib.Tag NewData;
            public DbTrack DbRepresentation;
        }

        private async Task UpdateFiles()
        {
            if(m_filesToUpdate.Count() < 1)
            {
                return;
            }

            using(MusicProvider mp = new MusicProvider())
            {
                /**
                 * Load the tags of all files that have been changed recently,
                 * *then* do the work in the database
                 * This avoids seeking HDDs back and forth between library and db
                 */

                var tracksToUpdate = await mp.AllTracks.Where( t =>
                    m_filesToUpdate.Contains(t.FileName)
                ).Select( t =>
                    new TrackToUpdate{
                        NewData = TagLib.File.Create(t.FileName).Tag,
                        DbRepresentation = t
                    }
                ).Where( t =>
                    !t.DbRepresentation.HasSameData(t.NewData)
                ).ToListAsync();

                foreach(var track in tracksToUpdate)
                {
                    track.DbRepresentation.SetTrackData(track.NewData);
                }

                foreach(var slice in tracksToUpdate.Select(t => t.DbRepresentation).GetSlices(SAVE_TO_DISK_INTERVAL))
                {
                    await mp.AllTracks.AddRangeAsync(slice);
                    await mp.SaveChangesAsync();
                }
            }
        }

        private async Task AddNewFiles()
        {
            if(m_filesToAdd.Count() < 1)
            {
                return;
            }

            using(MusicProvider mp = new MusicProvider())
            {
                foreach(var slice in m_filesToAdd.GetSlices(SAVE_TO_DISK_INTERVAL))
                {
                    var newTracks = new List<DbTrack>();
                    foreach(var trackFileName in slice)
                    {
                        var newTrack = new DbTrack{
                            FileName = trackFileName
                        };
                        newTrack.SetTrackData(TagLib.File.Create(trackFileName).Tag);
                        newTracks.Add(newTrack);
                    }
                    await mp.AllTracks.AddRangeAsync(newTracks);
                    await mp.SaveChangesAsync();
                }
            }
        }

        private async Task DeleteStaleDbEntries()
        {
            using(MusicProvider mp = new MusicProvider())
            {
                IQueryable<DbTrack> tracksToDelete = mp.AllTracks.Where( t =>
                    !File.Exists(t.FileName)
                );
                await Console.Out.WriteLineAsync($"LibraryMonitor: {tracksToDelete.Count()} tracks need to be removed from the database.");

                var i = 0;
                foreach(var track in tracksToDelete)
                {
                    mp.AllTracks.Remove(track);
                    if(i++ % SAVE_TO_DISK_INTERVAL == 0)
                    {
                        await mp.SaveChangesAsync();
                    }
                }
                // Update complete, set last update time
                mp.SetLastUpdateTime(DateTime.UtcNow.ToUniversalTime());
                await mp.SaveChangesAsync();
            }
        }
    }
}