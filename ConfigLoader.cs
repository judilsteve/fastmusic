using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace fastmusic
{
    // TODO This should use IConfiguration or something https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1
    /// <summary>
    /// Configuration object containing all user configurable data
    /// </summary>
    public class Config : IValidatableObject
    {
        /// <summary>
        /// URL to serve the API on
        /// </summary>
        [Required] public string? URL { get; set; } = null!;

        /// <summary>
        /// Full paths to all directories on disk where music should be streamed from
        /// </summary>
        [Required][MinLength(1)] public string[]? LibraryLocations{ get; set; } = null!;

        /// <summary>
        /// Mappings between file extensions and the MIME types they should be streamed with
        /// Files in @LibraryLocations with extensions not present here will be ignored by the application
        /// </summary>
        [Required][MinLength(1)] public Dictionary<string, string>? MimeTypes{ get; set; } = null!;

        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext _)
        {
            foreach(var libLoc in LibraryLocations!)
            {
                if(!Directory.Exists(libLoc))
                {
                    yield return new ValidationResult($"Library location {libLoc} does not exist", new[]{ nameof(LibraryLocations) });
                }
            }
        }
    }

    /// <summary>
    /// Responsible for loading the JSON configuration file from disk
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// Filename of the config file that the user is supposed to edit to their liking.
        /// </summary>
        private const string userConfigFile = "config.json";

        /// <summary>
        /// Filename of the default config file, which the user is supposed to leave alone,
        /// as a reference/fallback, but technically could do whatever they wanted to with.
        /// </summary>
        private const string defaultConfigFile = "config_default.json";

        /// <summary>
        /// Private instance for singleton pattern
        /// </summary>
        private static Config config;

         /// <summary>
         /// Singleton constructor. Will load config from disk if it is not already loaded.
         /// Once config is loaded, it will not be loaded again until application restart.
         /// </summary>
         /// <returns>The config object, as loaded from disk.</returns>
        public static async Task<Config> GetConfig()
        {
            if(config != null)
            {
                return config;
            }

            if(File.Exists(userConfigFile))
            {
                using var userConfigStream = File.OpenRead(userConfigFile);
                var userConfig = await JsonSerializer.DeserializeAsync<Config>(userConfigStream);
                if(ConfigIsValid(userConfig))
                {
                    config = userConfig;
                    return config;
                }
                else
                {
                    Console.Out.WriteLine($"Configuration file (\"{userConfigFile}\") was malformed, loading default config.");
                }
            }
            else
            {
                Console.Out.WriteLine($"Configuration file (\"{userConfigFile}\") not found, loading default config.");
            }

            using var defaultConfigStream = File.OpenRead(defaultConfigFile);
            config = await JsonSerializer.DeserializeAsync<Config>(defaultConfigStream); // TODO Why is this even a file and not just an object literal?
            return config;
        }
    }
}