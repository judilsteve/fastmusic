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

    public class ConfigLoader
    {
        private const string userConfig = "config.json";
        private const string defaultConfig = "config_default.json";

        public static Config LoadConfig()
        {
            Config config;
            if(File.Exists(userConfig))
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(userConfig));
                if(ConfigIsValid(config))
                {
                    return config;
                }
                else
                {
                    Console.Out.WriteLine($"Configuration file (\"{userConfig}\") was malformed, loading default config.");
                }
            }
            else
            {
                Console.Out.WriteLine($"Configuration file (\"{userConfig}\") not found, loading default config.");
            }
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(defaultConfig));
            return config;
        }

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