using fastmusic.DataProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fastmusic
{
    /// <summary>
    /// Startup class for the ASP.NET Core side of the program.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Here to satisfy ASP.NET core requirements.
        /// </summary>
        public Startup()
        {
        }

        /// <summary>
        /// Configures the services that will be available to the Web API classes
        /// via the dependency injector.
        /// </summary>
        /// <param name="services">A collection of services to be added to.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<MusicProvider>();

            services.AddSingleton<Configuration>(ConfigurationProvider.Configuration);

            services.AddControllers();

            // TODO Add Hangfire and set up recurring jobs
        }

        /// <summary>
        /// Configures the ASP.NET Core Web API.
        /// </summary>
        /// <param name="app">ASP.NET Core application builder, where configuration can be provided to the Web API on startup.</param>
        /// <param name="env">Information about the Web API's hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(e => e.MapControllers());
        }
    }
}
