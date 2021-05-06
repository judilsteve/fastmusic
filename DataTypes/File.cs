using fastmusic.Utils;

namespace fastmusic.DataTypes
{
    /// <summary>
    /// Represents any file in the file system
    /// </summary>
    public abstract class DbFile : IFilePath
    {
        /// <summary>
        /// Full path to the directory containing this file
        /// </summary>
        public string? FullPathToDirectory { get; set; }

        /// <summary>
        /// Full name of the file
        /// </summary>
        public string FileNameIncludingExtension { get; set; } = null!;
    }
}