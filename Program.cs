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
            CreateHostBuilder(args).Build().Run();

        /// <summary>
        /// Required by EF Core at design time to instantiate our DbContexts
        /// </summary>
        /// <param name="args">Command line arguments for the program.</param>
        /// <returns></returns>
        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls(Configuration.Instance.HostUrls);
    }
}
