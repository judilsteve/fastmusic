using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace fastmusic
{
    public class Config
    {
        public string URL { get; set; }

        public List<string> LibraryLocations{ get; set; }

        public Dictionary<string, string> MimeTypes{ get; set; }
    }

    public static class ConfigLoader
    {
        private const string userConfigFile = "config.json";
        private const string defaultConfigFile = "config_default.json";

        private static Config m_config;

        /**
         * @return The application configuration, as loaded from disk
         * Will be loaded from disk if it has not been already
         */
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

        /**
         * @return True iff. @param config has all required fields
         * and they are set to sensible values
         */
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