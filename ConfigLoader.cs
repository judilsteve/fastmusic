using System;
using System.Collections.Generic;
using System.IO;

namespace fastmusic
{
    public class Config
    {
        public string URL { get; set; }

        public List<string> LibraryLocations{ get; set; } = new List<string>();

        public List<string> FileTypes{ get; set; } = new List<string>();
    }
    public class ConfigLoader
    {
        private const string userConfig = "config.txt";
        private const string defaultConfig = "config_default.txt";
        public static Config LoadConfig()
        {
            if(File.Exists(userConfig))
            {
                var config = LoadConfig(userConfig);
                if(config != null)
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
            return LoadConfig(defaultConfig);
        }

        private static Config LoadConfig(string configFileName)
        {
            FileStream configFile = File.OpenRead(configFileName);
            var conf = new Config();
            using(var reader = new StreamReader(configFile))
            {
                uint lineNo = 0;
                while(!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    lineNo++;

                    if(line.Trim(' ').Trim('\t') == "")
                    {
                        // Blank line
                        continue;
                    }

                    var pair = reader.ReadLine().Split('=');

                    if(pair[0].StartsWith('#'))
                    {
                        // Comment
                        continue;
                    }

                    if(pair.Length != 2)
                    {
                        Console.Error.WriteLine($"Malformed configuration pair at line {lineNo} of {configFileName}");
                        continue;
                    }

                    switch(pair[0])
                    {
                    case "URL":
                        conf.URL = pair[1].Trim('"');
                        continue;
                    case "LibraryLocations":
                        var locations = pair[1].Split(';');
                        foreach (var location in locations)
                        {
                            conf.LibraryLocations.Add(location.Trim('"'));
                        }
                        continue;
                    case "FileTypes":
                        var types = pair[1].Split(';');
                        foreach (var type in types)
                        {
                            conf.FileTypes.Add(type.Trim('"'));
                        }
                        continue;
                    default:
                        Console.Error.WriteLine($"Unrecognised setting \"{pair[0]}\" (line {lineNo})");
                        continue;
                    }
                }
            }
            return conf;
        }
    }
}