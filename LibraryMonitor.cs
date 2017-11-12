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

        private Func<MusicProvider> m_getMusicProvider;

        private Timer m_syncTimer;

        public static void StartMonitoring(Func<MusicProvider> getMusicProvider, List<String> libraryLocations, List<String> fileTypes)
        {
            if(m_instance != null) return;
            m_instance = new LibraryMonitor(getMusicProvider, libraryLocations, fileTypes);
        }

        private LibraryMonitor(Func<MusicProvider> getMusicProvider, List<String> libraryLocations, List<String> fileTypes)
        {
            m_libraryLocations = libraryLocations;
            m_fileTypes = fileTypes;
            m_getMusicProvider = getMusicProvider;

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
            await Console.Out.WriteLineAsync("SynchroniseDb: Beginning update (enumerating files).");
            var allTrackFileNames = new List<String>();
            foreach(var libraryLocation in m_libraryLocations)
            {
                // FileSystemWatcher doesn't support watching multiple file patterns
                // A separate watcher object must be created for each file type
                foreach(var fileType in m_fileTypes)
                {
                    allTrackFileNames.AddRange(
                        Directory.EnumerateFiles(libraryLocation, $"*.{fileType}", SearchOption.AllDirectories)
                    );
                }
            }
            await Console.Out.WriteLineAsync("SynchroniseDb: Finished enumerating files, running update.");
            await UpdateFiles(allTrackFileNames);
            await Console.Out.WriteLineAsync("SynchroniseDb: Finished updating.");
        }

        private async Task UpdateFiles(List<String> trackFileNames)
        {
            using(MusicProvider mp = m_getMusicProvider())
            {

            int i = 0; // Used to periodically save changes to the db

            foreach(var trackFileName in trackFileNames)
            {
                if(await mp.AllTracks.AnyAsync(t => t.FileName == trackFileName))
                {
                    // Track already exists in the database, update it
                    var trackToUpdate = await mp.AllTracks.FirstAsync( t =>
                        t.FileName == trackFileName
                    );
                    var tag = TagLib.File.Create(trackFileName).Tag;
                    if(!trackToUpdate.HasSameData(tag))
                    {
                        await Console.Out.WriteLineAsync($"LibraryMonitor: Updating: {trackFileName}.");
                        trackToUpdate.SetTrackData(tag);
                        mp.AllTracks.Update(trackToUpdate);
                    }
                }
                else
                {
                    await Console.Out.WriteLineAsync($"LibraryMonitor: Adding: {trackFileName}.");
                    // New track. Add to database
                    var newTrack = new DbTrack();
                    newTrack.SetTrackData(TagLib.File.Create(trackFileName).Tag);
                    await mp.AllTracks.AddAsync(newTrack);
                }

                if(i++ % 4096 == 0)
                {
                    await mp.SaveChangesAsync();
                }
            }

            var tracksToDelete = mp.AllTracks.Where( t =>
                ! (trackFileNames.Any( fileName =>
                    t.FileName == fileName
                ))
            );

            i = 0;
            foreach(var track in tracksToDelete)
            {
                await Console.Out.WriteLineAsync($"LibraryMonitor: Removing: {track.FileName}.");
                mp.AllTracks.Remove(track);
                if(i++ % 4096 == 0)
                {
                    await mp.SaveChangesAsync();
                }
            }

            }
        }

        private async Task UpdateTrack(string trackFileName)
        {
            using(MusicProvider mp = m_getMusicProvider())
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
            using(MusicProvider mp = m_getMusicProvider())
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
            using(MusicProvider mp = m_getMusicProvider())
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
            using(MusicProvider mp = m_getMusicProvider())
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