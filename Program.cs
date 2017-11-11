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

            // TODO Make this run periodically instead of just once
            var dbUpdateThread = new Thread(musicProvider.UpdateDb);
            dbUpdateThread.Start();

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://192.168.0.7:5000")
                .UseStartup<Startup>()
                .Build();
    }
}
