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

            BuildWebHost(args, config).Run();
        }

        public static IWebHost BuildWebHost(string[] args, Config config) =>
            WebHost.CreateDefaultBuilder(args)
                .UseUrls(config.URL)
                .UseStartup<Startup>()
                .Build();
    }
}
