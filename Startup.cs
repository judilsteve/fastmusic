using fastmusic.DataProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.Console;
using Hangfire.Dashboard.Dark;
using Hangfire.Storage.SQLite;

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
            services.AddDbContext<MusicContext>();

            services.AddSingleton<Configuration>(Configuration.Instance);

            services.AddHangfire(c => c
                .UseSQLiteStorage("fastmusic_hangfire.db")
                .UseConsole()
                .UseDarkDashboard());

            services.AddControllers();
        }

        /// <summary>
        /// Configures the ASP.NET Core Web API.
        /// </summary>
        /// <param name="app">ASP.NET Core application builder, where configuration can be provided to the Web API on startup.</param>
        /// <param name="env">Information about the Web API's hosting environment.</param>
        /// <param name="musicContext">Allows migrating the database on startup</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MusicContext musicContext)
        {
            musicContext.Database.Migrate();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHangfireServer();
            var everySecondMinute = "*/2 * * * *";
            // TODO Retry policies
            RecurringJob.AddOrUpdate<LibraryMonitor>(m => m.SynchroniseDb(null!, default), everySecondMinute);

            app.UseHangfireDashboard();

            app.UseRouting();

            app.UseEndpoints(e => e.MapControllers());
        }
    }
}
