using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace fastmusic
{
    /// <summary>
    /// Configuration object containing all user configurable data
    /// </summary>
    public class Config
    {
        /// <summary>
        /// URL to serve the API on
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// Full paths to all directories on disk where music should be streamed from
        /// </summary>
        public List<string> LibraryLocations{ get; set; }

        /// <summary>
        /// Mappings between file extensions and the MIME types they should be streamed with
        /// Files in @LibraryLocations with extensions not present here will be ignored by the application
        /// </summary>
        public Dictionary<string, string> MimeTypes{ get; set; }
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
        private static Config m_config;

         /// <summary>
         /// Singleton constructor. Will load config from disk if it is not already loaded.
         /// Once config is loaded, it will not be loaded again until application restart.
         /// </summary>
         /// <returns>The config object, as loaded from disk.</returns>
        public static Config GetConfig()
        {
            if(m_config != null)
            {
                return m_config;
            }

            if(File.Exists(userConfigFile))
            {
                Config userConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(userConfigFile));
                if(ConfigIsValid(userConfig))
                {
                    m_config = userConfig;
                    return m_config;
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

            m_config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(defaultConfigFile));
            return m_config;
        }

         /// <summary>
         /// Determines if a configuration object contains sensible data (data that will not crash the program)
         /// </summary>
         /// <param name="config"></param>
         /// <returns>True iff. @param config has all required fields and they are set to sensible values</returns>
        private static bool ConfigIsValid(Config config)
        {
            if(config.URL == null)
            {
                Console.Error.WriteLine("Configuration file must specify a URL");
                return false;
            }
            if(config.LibraryLocations.Count < 1)
            {
                Console.Error.WriteLine("Configuration file must specify at least one library location");
                return false;
            }
            foreach(var libLoc in config.LibraryLocations)
            {
                if(!Directory.Exists(libLoc))
                {
                    Console.Error.WriteLine($"Library location {libLoc} does not exist");
                    return false;
                }
            }
            if(config.MimeTypes.Count < 1)
            {
                Console.Error.WriteLine("Configuration file must specify at least one mime type");
                return false;
            }
            return true;
        }
    }
}