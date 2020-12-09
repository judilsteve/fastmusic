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
        /// Absolute host URL for the web service
        /// </summary>
        /// <value></value>
        public string URL { get; set; } = null!;

        /// <summary>
        /// Absolute paths to library directories on disk
        /// </summary>
        /// <value></value>
        public string[] LibraryLocations { get; set; } = null!;

        /// <summary>
        /// Mapping from file extension to mime type. File extensions are specified without leading "." characters
        /// </summary>
        /// <value></value>
        public Dictionary<string, string> MimeTypes { get; set; } = null!;
    }

    /// <summary>
    /// Responsible for loading the JSON configuration file from disk
    /// </summary>
    public static class ConfigurationProvider
    {
        private class ConfigurationDto : IValidatableObject
        {
            [Required] public string? URL { get; set; } = null!;
            [Required][MinLength(1)] public string[]? LibraryLocations{ get; set; } = null!;
            [Required][MinLength(1)] public Dictionary<string, string>? MimeTypes{ get; set; } = null!;

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
        /// Filename of the config file that the user is supposed to edit to their liking.
        /// </summary>
        private const string configurationFileName = "configuration.json";

        /// <summary>
        /// The configuration, as loaded at program startup
        /// </summary>
        public static Configuration Configuration = LoadConfiguration();

        /// <summary>
        /// Constructor. Loads configuration from json file
        /// </summary>
        private static Configuration LoadConfiguration()
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

            return new Configuration
            {
                URL = configuration.URL!,
                LibraryLocations = configuration.LibraryLocations!,
                MimeTypes = configuration.MimeTypes!
                    // Trim leading dots from file extensions
                    .ToDictionary(kvp => kvp.Key.TrimStart('.'), kvp => kvp.Value)
            };
        }
    }
}