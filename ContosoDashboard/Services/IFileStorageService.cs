namespace ContosoDashboard.Services;

/// <summary>
/// Abstraction for file storage operations
/// Enables local/Azure storage swap for future cloud migration
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    /// <param name="fileStream">Stream containing the file data</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="relativePath">Relative path for storage (e.g., userId/projectId/guid.ext)</param>
    /// <returns>Full relative path where file is stored</returns>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string relativePath);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    /// <param name="relativePath">Relative path of the file to delete</param>
    Task DeleteAsync(string relativePath);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    /// <param name="relativePath">Relative path of the file to download</param>
    /// <returns>Stream containing the file data</returns>
    Task<Stream> DownloadAsync(string relativePath);

    /// <summary>
    /// Get a URL for accessing a file (for preview/download)
    /// </summary>
    /// <param name="relativePath">Relative path of the file</param>
    /// <param name="expiration">Time until URL expires</param>
    /// <returns>URL for accessing the file</returns>
    Task<string> GetUrlAsync(string relativePath, TimeSpan expiration);

    /// <summary>
    /// Get total storage used for a base path
    /// </summary>
    /// <param name="basePath">Base path to calculate storage for</param>
    /// <returns>Total bytes used</returns>
    Task<long> GetStorageUsedAsync(string basePath);
}