using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.Threading.Tasks;

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
        public static async Task Main(string[] args)
        {
            var config = ConfigurationProvider.Configuration;
            var libMon = LibraryMonitor.GetInstance(config.LibraryLocations, config.MimeTypes.Keys.ToList());

            BuildWebHost(args, config).Run();
        }

        /// <summary>
        /// Builds the ASP.NET core side of the program.
        /// </summary>
        /// <param name="args">Command line arguments for the program.</param>
        /// <param name="config">User configuration (loaded from disk).</param>
        /// <returns></returns>
        public static IWebHost BuildWebHost(string[] args, Configuration config) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(config.URL)
                .UseStartup<Startup>()
                .Build();
    }
}
