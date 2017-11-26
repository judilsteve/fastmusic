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
    /// <summary>
    /// Entry point class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point method
        /// </summary>
        /// <param name="args">Command line arguments for the program.</param>
        public static void Main(string[] args)
        {
            var config = ConfigLoader.GetConfig();
            var libMon = LibraryMonitor.GetInstance(config);

            BuildWebHost(args, config).Run();
        }

        /// <summary>
        /// Builds the ASP.NET core side of the program.
        /// </summary>
        /// <param name="args">Command line arguments for the program.</param>
        /// <param name="config">User configuration (loaded from disk).</param>
        /// <returns></returns>
        public static IWebHost BuildWebHost(string[] args, Config config) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(config.URL)
                .UseStartup<Startup>()
                .Build();
    }
}
