using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace fastmusic
{
    /// <summary>
    /// Application configuration
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        /// <returns></returns>
        public static Configuration Instance = new Configuration("configuration.json");

        /// <summary>
        /// Absolute host URLs for the web service
        /// </summary>
        /// <value></value>
        public string[] HostUrls { get; init; }

        /// <summary>
        /// Absolute paths to library directories on disk
        /// </summary>
        /// <value></value>
        public string[] LibraryLocations { get; init; }

        /// <summary>
        /// File names to use for album art (e.g. "front.jpg").
        /// If a track can be associated with two different art files,
        /// the one that matches the earliest pattern in this array will be chosen.
        /// </summary>
        /// <value></value>
        public string[] AlbumArtFileNames { get; init; }

        /// <summary>
        /// Mapping from file extension to mime type. File extensions are specified without leading "." characters
        /// </summary>
        /// <value></value>
        public Dictionary<string, string> MimeTypes { get; init; }

        private class ConfigurationDto : IValidatableObject
        {
            [Required] public string[]? HostUrls { get; set; }
            [Required][MinLength(1)] public string[]? LibraryLocations{ get; set; }
            public string[]? AlbumArtFileNames { get; set; }
            [Required][MinLength(1)] public Dictionary<string, string>? MimeTypes{ get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext? _)
            {
                foreach(var libLoc in LibraryLocations!)
                {
                    if(!Directory.Exists(libLoc))
                    {
                        yield return new ValidationResult($"Library location \"{libLoc}\" does not exist", new[]{ nameof(LibraryLocations) });
                    }
                }
            }
        }

        /// <summary>
        /// Constructor. Loads configuration from json file
        /// </summary>
        private Configuration(string configurationFileName)
        {
            if(!File.Exists(configurationFileName))
            {
                throw new Exception($"Configuration file (\"{configurationFileName}\") not found");
            }

            var configuration = JsonSerializer.Deserialize<ConfigurationDto?>(File.ReadAllText(configurationFileName))
                ?? throw new Exception("Configuration was null");

            var validationContext = new ValidationContext(configuration);
            var validationErrors = new List<ValidationResult>();
            if(!Validator.TryValidateObject(configuration, validationContext, validationErrors))
            {
                var formattedErrors = validationErrors
                    .Select(e => $"{e.ErrorMessage} (keys: {string.Join(", ", e.MemberNames)})")
                    .ToArray();
                throw new Exception($"Configuration was invalid. Validation errors: \r\n\t{string.Join("\r\n\t", formattedErrors)}");
            }

            HostUrls = configuration.HostUrls!;
            LibraryLocations = configuration.LibraryLocations!;
            AlbumArtFileNames = configuration.AlbumArtFileNames ?? Array.Empty<string>();
            MimeTypes = configuration.MimeTypes!
                // Trim leading dots from file extensions
                .ToDictionary(kvp => kvp.Key.TrimStart('.'), kvp => kvp.Value);
        }
    }
}