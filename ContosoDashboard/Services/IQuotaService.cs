namespace ContosoDashboard.Services;

/// <summary>
/// Interface for storage quota management
/// </summary>
public interface IQuotaService
{
    /// <summary>
    /// Check if a user can upload a file of given size
    /// </summary>
    Task<bool> CanUploadAsync(int userId, long fileSize);

    /// <summary>
    /// Check if total system quota would be exceeded
    /// </summary>
    Task<bool> IsTotalQuotaExceededAsync(long fileSize);

    /// <summary>
    /// Get current storage used by a user
    /// </summary>
    Task<long> GetUserStorageUsedAsync(int userId);

    /// <summary>
    /// Get storage quota for a user
    /// </summary>
    Task<long> GetUserStorageQuotaAsync(int userId);

    /// <summary>
    /// Get total storage used across all users
    /// </summary>
    Task<long> GetTotalStorageUsedAsync();

    /// <summary>
    /// Get total system storage quota
    /// </summary>
    Task<long> GetTotalStorageQuotaAsync();

    /// <summary>
    /// Update user's storage used (positive = add, negative = subtract)
    /// </summary>
    Task UpdateUserStorageAsync(int userId, long delta);
}