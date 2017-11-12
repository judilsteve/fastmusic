using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.EntityFrameworkCore;

using fastmusic.DataProviders;
using fastmusic.DataTypes;

namespace fastmusic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = ConfigLoader.LoadConfig();

            var mediaTypeProviderOptionsBuilder = new DbContextOptionsBuilder<MediaTypeProvider>();
            mediaTypeProviderOptionsBuilder.UseInMemoryDatabase(MediaTypeProvider.m_dbName);
            var types = new MediaTypeProvider(mediaTypeProviderOptionsBuilder.Options);
            foreach(var pair in config.MimeTypes)
            {
                var type = new DbMediaType {
                    Extension = pair.Key,
                    MimeType = pair.Value
                };
                types.Types.Add(type);
                Console.Out.WriteLine($"Registering media type: {type.Extension} -> {type.MimeType}");
            }
            types.SaveChanges();

            // TODO This doesn't actually monitor the library, it finishes the update and then its internal instance gets destroyed
            LibraryMonitor.StartMonitoring(config.LibraryLocations, config.MimeTypes.Keys.ToList());

            BuildWebHost(args, config).Run();
        }

        public static IWebHost BuildWebHost(string[] args, Config config) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(config.URL)
                .UseStartup<Startup>()
                .Build();
    }
}
