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

using fastmusic.DataProviders;

namespace fastmusic
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var musicProvider = new MusicProvider();

            var config = ConfigLoader.LoadConfig();

            // TODO Make this run periodically instead of just once
            var dbUpdateThread = new Thread(() => musicProvider.UpdateDb(config.LibraryLocations, config.FileTypes));
            dbUpdateThread.Start();

            BuildWebHost(args, config).Run();
        }

        public static IWebHost BuildWebHost(string[] args, Config config) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(config.URL)
                .UseStartup<Startup>()
                .Build();
    }
}
