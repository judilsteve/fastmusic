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
    /// <summary>
    /// Monitors the user-configured library directories at a user-configured interval,
    /// scanning them for new, updated, or deleted files.
    /// </summary>
    public class LibraryMonitor
    {
        /// <summary>
        /// Private instance used for singleton pattern
        /// </summary>
        private static LibraryMonitor m_instance;

        /// <summary>
        /// List of full paths to all music library locations on disk
        /// </summary>
        private List<string> m_libraryLocations;

        /// <summary>
        /// List of wildcard patterns that will be used to monitor
        /// files of certain extensions in the library
        /// </summary>
        private List<string> m_filePatterns = new List<string>();

        /// <summary>
        /// List of wildcard patterns that will be used to monitor
        /// album art files in the library
        /// </summary>
        private List<string> m_albumArtPatterns = new List<string>();

        /// <summary>
        /// List of full paths to music files that have changed on disk
        /// since the last database sync.
        /// </summary>
        private HashSet<string> m_filesToUpdate = new HashSet<string>();

        /// <summary>
        /// List of full paths to music files that have been created since
        /// the last database sync.
        /// </summary>
        private List<string> m_filesToAdd = new List<string>();

        /// <summary>
        /// List of full paths to album art files that have been created/updated
        /// since the last database sync.
        /// </summary>
        private HashSet<string> m_albumArtToUpdate = new HashSet<string>();

        /// <summary>
        /// Enables the library monitor to sync the database with the library periodically.
        /// </summary>
        private Timer m_syncTimer;

        /// <summary>
        /// Interval in seconds between the end of the last database sync
        /// and the starty of the next one.
        /// </summary>
        private const int SYNC_INTERVAL_SECONDS = 120; // TODO Make this user configurable

        /// <summary>
        /// Batch size for adding, removing, or updating records.
        /// </summary>
        private const int SAVE_TO_DISK_INTERVAL = 2048;

        /// <summary>
        /// Singleton constructor. LibraryMonitor will be created if it does not already exist.
        /// Once created, the instance lives until the program is terminated.
        /// </summary>
        /// <param name="config">User configuration, as loaded from disk.</param>
        /// <returns>An instance of the library monitor</returns>
        public static LibraryMonitor GetInstance(Config config)
        {
            if(m_instance == null) m_instance = new LibraryMonitor(config);
            return m_instance;
        }

         /// <summary>
         /// Sets up a routine that monitors all files of type @param fileTypes
         /// in all directories in @param libraryLocations.
         /// The routine will run immediately upon construction, then at a specified interval,
         /// always in a separate thread.
         /// </summary>
         /// <param name="config">User configuration, as loaded from disk.</param>
        private LibraryMonitor(Config config)
        {
            m_filePatterns = config.MimeTypes.Keys.Select( fileType =>
                $"*.{fileType}"
            ).ToList();
            m_albumArtPatterns = config.AlbumArtPatterns;

            // Schedule a task to synchronise filesystem and DB every so often
            // We use Timeout.Infinite to avoid multiple syncs running concurrently
            // This is changed at the end of SynchroniseDb
            m_syncTimer = new Timer(async (o) => await SynchroniseDb(), null, 0, Timeout.Infinite);
        }

         /// <summary>
         /// Checks the configured library locations for new/updated/deleted files,
         /// and updates the database accordingly
         /// </summary>
        private async Task SynchroniseDb()
        {
            await Console.Out.WriteLineAsync("LibraryMonitor: Starting update (enumerating files).");
            using(var mp = new MusicProvider())
            {
                var lastDBUpdateTime = mp.GetLastUpdateTime();
                // Set the last update time now
                // Otherwise, files that change between now and update completion
                // might not get flagged for update up in the next sync round
                await mp.SetLastUpdateTime(DateTime.UtcNow.ToUniversalTime());

                foreach(var libraryLocation in m_libraryLocations)
                {
                    await FindFilesToUpdate(libraryLocation, mp, lastDBUpdateTime);
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

         /// <summary>
         /// Recursively adds all files in @param startDirectory (of the configured file types)
         /// that have been created or modified since @lastDBUpdateTime
         /// to the list of files that need to be updated in/added to the database
         /// </summary>
         /// <param name="startDirectory">Where to start looking for new/updated files</param>
         /// <param name="mp">Handle to the database</param>
         /// <param name="lastDBUpdateTime">Write time beyond which files will be condsidered new or modified</param>
         /// <returns></returns>
        private async Task FindFilesToUpdate(
            string startDirectory,
            MusicProvider mp,
            HashSet<string> albumArtFiles,
            DateTime lastDBUpdateTime
        )
        {
            // Recursively call this function on sub-directories that have been written to since the last update
            foreach(var subDir in Directory.EnumerateDirectories(startDirectory))
            {
                if(new DirectoryInfo(subDir).LastWriteTime > lastDBUpdateTime)
                {
                    await FindFilesToUpdate(subDir, mp, albumArtFiles, lastDBUpdateTime);
                }
            }

            string artFileName;
            // Look for album art in this directory
            foreach(var albumArtPattern in m_albumArtPatterns)
            {
                // Each track only supports one piece of album art
                // Pick the first one that we find
                artFileName = Directory.EnumerateFiles(startDirectory, albumArtPattern, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if(artFileName != null)
                {
                    break;
                }
            }

            // Look for files to update in this directory
            foreach(var filePattern in m_filePatterns)
            {
                foreach(var file in Directory.EnumerateFiles(startDirectory, filePattern, SearchOption.TopDirectoryOnly))
                {
                    if(!(await mp.AllTracks.AsNoTracking().AnyAsync( t => t.FileName == file )))
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

         /// <summary>
         /// Intermediate data structure holding the db representation of a track file
         /// and the filesystem representaiton of the track file
         /// Used by UpdateFiles
         /// </summary>
        private class TrackToUpdate {

            /// <summary>
            /// Fresh track metadata, as loaded from disk
            /// </summary>
            public TagLib.Tag NewData;

            /// <summary>
            /// Possibly stale database represenation of the track metadata
            /// </summary>
            public DbTrack DbRepresentation;
        }

         /// <summary>
         /// Synchronises all database rows selected for update with the file on disk
         /// Clears db rows selected for update
         /// </summary>
        private async Task UpdateFiles()
        {
            if(m_filesToUpdate.Count() < 1)
            {
                return;
            }

            IQueryable<TrackToUpdate> tracksToUpdate;
            using(var mp = new MusicProvider())
            {
                // Load the tags of all files that have been changed recently,
                // *then* do the work in the database
                // This avoids seeking HDDs back and forth between library and db

                tracksToUpdate = mp.AllTracks.Where( t =>
                    m_filesToUpdate.Contains(t.FileName)
                ).Select( t =>
                    new TrackToUpdate{
                        NewData = TagLib.File.Create(t.FileName).Tag,
                        DbRepresentation = t
                    }
                ).Where( t =>
                    !t.DbRepresentation.HasSameData(t.NewData)
                );
            }

            // TODO New context for each slice
            foreach(var slice in tracksToUpdate.Select(t => t.DbRepresentation).GetSlices(SAVE_TO_DISK_INTERVAL))
            {
                using(var mp = new MusicProvider())
                {
                    mp.AllTracks.UpdateRange(slice);
                    await mp.SaveChangesAsync();
                }
            }
        }

         /// <summary>
         /// Adds all new files marked for addition to the database
         /// Clears files marked for addition
         /// </summary>
        private async Task AddNewFiles()
        {
            if(m_filesToAdd.Count() < 1)
            {
                return;
            }

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
                using(MusicProvider mp = new MusicProvider())
                {
                    await mp.AllTracks.AddRangeAsync(newTracks);
                    await mp.SaveChangesAsync();
                }
            }
        }

         /// <summary>
         /// Removes all files from the database which no longer exist on disk
         /// </summary>
        private async Task DeleteStaleDbEntries()
        {
            using(var mp = new MusicProvider())
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
                await mp.SaveChangesAsync();
            }
        }
    }
}