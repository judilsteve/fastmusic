using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using fastmusic.DataProviders;

namespace fastmusic
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MusicProvider>();
            services.AddDbContext<MediaTypeProvider>();
            services.Add(new ServiceDescriptor(typeof(Config), ConfigLoader.LoadConfig()));
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // TODO This doesn't seem to actually monitor the library, it finishes the big update and then the FileSystemMonitors don't seem to respond to any changes
            Func<MusicProvider> getMusicProvider = () => app.ApplicationServices.GetService<MusicProvider>();
            var config = app.ApplicationServices.GetService<Config>();
            LibraryMonitor.StartMonitoring(getMusicProvider, config.LibraryLocations, config.MimeTypes.Keys.ToList());

            app.UseMvc();
        }
    }
}
