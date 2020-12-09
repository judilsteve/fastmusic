using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
        public static void Main(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(ConfigurationProvider.Configuration.URL)
                .UseStartup<Startup>()
                .Build();
    }
}
