using System.IO;

namespace fastmusic.Utils
{
    /// <summary>
    /// Barebones struct implementation of IFilePath
    /// </summary>
    public struct FilePath : IFilePath
    {
        /// <summary>
        /// Full path to the directory containing this file
        /// </summary>
        public string? FullPathToDirectory { get; }

        /// <summary>
        /// Full name of the file
        /// </summary>
        public string FileNameIncludingExtension { get; }

        /// <summary>
        /// Constructs a split path from a FileInfo object
        /// </summary>
        /// <param name="fileInfo"></param>
        public FilePath(FileInfo fileInfo)
        {
            FullPathToDirectory = fileInfo.Directory?.FullName;
            FileNameIncludingExtension = fileInfo.Name;
        }

        /// <summary>
        /// Constructs a split path from a directory path and a file name
        /// </summary>
        /// <param name="fullPathToDirectory"></param>
        /// <param name="fileNameIncludingExtension"></param>
        public FilePath(string? fullPathToDirectory, string fileNameIncludingExtension)
        {
            FullPathToDirectory = fullPathToDirectory;
            FileNameIncludingExtension = fileNameIncludingExtension;
        }
    }

    /// <summary>
    /// Interface for representing file paths with directory and filename separated
    /// </summary>
    public interface IFilePath
    {
        /// <summary>
        /// Full path to the directory containing this file
        /// </summary>
        string? FullPathToDirectory { get; }

        /// <summary>
        /// Full name of the file
        /// </summary>
        string FileNameIncludingExtension { get; }
    }

    /// <summary>
    /// Useful extension methods for file paths
    /// </summary>
    public static class IFilePathExtensions
    {
        /// <summary>
        /// Complete path to the file (combines directory and filename)
        /// </summary>
        public static string CompletePath(this IFilePath path) => path.FullPathToDirectory != null
            ? Path.Combine(path.FullPathToDirectory, path.FileNameIncludingExtension)
            : path.FileNameIncludingExtension;
    }
}